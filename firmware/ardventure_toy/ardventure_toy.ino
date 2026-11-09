#include <Arduino.h>
#include <WiFi.h>
#include <WebServer.h>
#include <esp_camera.h>
#include <img_converters.h>
#include <ESP_I2S.h>
#include <array>
#include <memory>
#include "ColourSampler.h"
#include "SoundMeter.h"

namespace {
constexpr char kAccessPoint[] = "ARdventure-Toy";
// Demo-only access point; no home-network credentials are needed or stored.
constexpr char kPassword[] = "mosswood-demo";
constexpr std::size_t kWidth = 320, kHeight = 240;
constexpr std::size_t kRgbBytes = kWidth * kHeight * 3;
WebServer server(80);
bool cameraReady = false;
I2SClass microphone(I2S_NUM_0);
bool microphoneReady = false;
unsigned long readyAt = 0;
std::uint32_t sequence = 0;

struct ReturnFrame {
    void operator()(camera_fb_t* frame) const { if (frame) esp_camera_fb_return(frame); }
};
using Frame = std::unique_ptr<camera_fb_t, ReturnFrame>;
using PixelBuffer = std::unique_ptr<std::uint8_t, decltype(&free)>;
PixelBuffer pixels(nullptr, &free);

void reply(int status, const char* body) {
    server.sendHeader("Cache-Control", "no-store");
    server.send(status, "application/json", body);
}

bool warmedUp() { return cameraReady && static_cast<long>(millis() - readyAt) >= 0; }

int listen(char* json, std::size_t capacity) {
    if (!microphoneReady) { snprintf(json, capacity, "{\"error\":\"microphone_not_ready\"}"); return 503; }
    std::array<std::int16_t, 320> block{};
    std::size_t received = 0;
    // Discard queued audio so the requested window describes what happens now.
    for (int i = 0; i < 12; ++i) {
        i2s_channel_read(microphone.rxChan(), block.data(), sizeof(block), &received, 0);
        if (received == 0) break;
    }
    ardventure::SoundMeter meter;
    const auto deadline = millis() + 3500;
    while (meter.samples() < 32000 && static_cast<long>(millis() - deadline) < 0) {
        received = 0;
        const auto error = i2s_channel_read(microphone.rxChan(), block.data(), sizeof(block), &received, 100);
        if (error != ESP_OK && error != ESP_ERR_TIMEOUT) break;
        if (received > 0 && (received % sizeof(std::int16_t) != 0 || !meter.add(block.data(), received / sizeof(std::int16_t)))) break;
        delay(1);
    }
    if (meter.samples() != 32000) { snprintf(json, capacity, "{\"error\":\"microphone_read_failed\"}"); return 503; }
    snprintf(json, capacity,
             "{\"version\":1,\"source\":\"xiao-microphone\",\"sequence\":%lu,"
             "\"rms\":%.7f,\"peak_rms\":%.7f,\"clipped_fraction\":%.7f,"
             "\"samples\":32000,\"sample_rate\":16000,\"duration_ms\":2000}",
             static_cast<unsigned long>(++sequence), meter.rms(), meter.peak(), meter.clippedFraction());
    return 200;
}

int capture(char* json, std::size_t capacity) {
    if (!warmedUp()) { snprintf(json, capacity, "{\"error\":\"camera_not_ready\"}"); return 503; }
    const auto started = millis();
    // Drop the buffered frame; the next acquisition is a more recent exposure.
    { Frame stale(esp_camera_fb_get()); }
    Frame frame(esp_camera_fb_get());
    if (!frame) { snprintf(json, capacity, "{\"error\":\"capture_failed\"}"); return 503; }
    if (frame->format != PIXFORMAT_JPEG || frame->width != kWidth || frame->height != kHeight) {
        snprintf(json, capacity, "{\"error\":\"unexpected_frame\"}"); return 500;
    }
    if (!fmt2rgb888(frame->buf, frame->len, frame->format, pixels.get())) {
        snprintf(json, capacity, "{\"error\":\"decode_failed\"}"); return 500;
    }
    ardventure::ColourReading colour;
    if (!ardventure::sampleCentreBgr(pixels.get(), kRgbBytes, kWidth, kHeight, colour)) {
        snprintf(json, capacity, "{\"error\":\"sample_failed\"}"); return 500;
    }
    snprintf(json, capacity,
             "{\"version\":1,\"source\":\"xiao-camera\",\"sequence\":%lu,"
             "\"r\":%u,\"g\":%u,\"b\":%u,\"samples\":%lu,\"capture_ms\":%lu}",
             static_cast<unsigned long>(++sequence), colour.red, colour.green, colour.blue,
             static_cast<unsigned long>(colour.samples), millis() - started);
    return 200;
}

void scan() {
    char json[256];
    const auto status = capture(json, sizeof(json));
    reply(status, json);
    Serial.println(json);
}

bool initialiseCamera() {
    if (!psramFound()) { Serial.println("ERROR: enable OPI PSRAM for XIAO ESP32S3."); return false; }
    camera_config_t config{};
    // XIAO ESP32-S3 Sense pinout from Seeed/Espressif's CameraWebServer example.
    config.pin_pwdn = -1; config.pin_reset = -1;
    config.pin_xclk = 10; config.pin_sccb_sda = 40; config.pin_sccb_scl = 39;
    config.pin_d0 = 15; config.pin_d1 = 17; config.pin_d2 = 18; config.pin_d3 = 16;
    config.pin_d4 = 14; config.pin_d5 = 12; config.pin_d6 = 11; config.pin_d7 = 48;
    config.pin_vsync = 38; config.pin_href = 47; config.pin_pclk = 13;
    config.ledc_channel = LEDC_CHANNEL_0; config.ledc_timer = LEDC_TIMER_0;
    config.xclk_freq_hz = 20000000;
    config.pixel_format = PIXFORMAT_JPEG;
    config.frame_size = FRAMESIZE_QVGA;
    config.jpeg_quality = 12;
    config.fb_count = 2;
    config.grab_mode = CAMERA_GRAB_LATEST;
    config.fb_location = CAMERA_FB_IN_PSRAM;
    const auto error = esp_camera_init(&config);
    if (error != ESP_OK) { Serial.printf("Camera init failed: 0x%x\n", error); return false; }
    pixels.reset(static_cast<std::uint8_t*>(ps_malloc(kRgbBytes)));
    if (!pixels) { esp_camera_deinit(); Serial.println("RGB buffer allocation failed."); return false; }
    const auto sensor = esp_camera_sensor_get();
    Serial.printf("Camera PID: 0x%x; PSRAM: %u bytes\n", sensor->id.PID, ESP.getPsramSize());
    readyAt = millis() + 2000; // Allow automatic exposure/white balance to settle.
    return true;
}
} // namespace

void setup() {
    Serial.begin(115200);
    // USB serial is optional: never wait indefinitely for a monitor to connect.
    const auto serialStart = millis();
    while (!Serial && millis() - serialStart < 1500) delay(10);
    cameraReady = initialiseCamera();
    // Sense PDM microphone: CLK GPIO42, DATA GPIO41 (Seeed's Arduino 3.x example).
    microphone.setPinsPdmRx(42, 41);
    microphoneReady = microphone.begin(I2S_MODE_PDM_RX, 16000, I2S_DATA_BIT_WIDTH_16BIT, I2S_SLOT_MODE_MONO);
    Serial.println(microphoneReady ? "Microphone ready: 16 kHz mono." : "ERROR: microphone init failed.");
    WiFi.mode(WIFI_AP);
    if (!WiFi.softAP(kAccessPoint, kPassword, 1, false, 1)) {
        Serial.println("ERROR: access point startup failed."); return;
    }
    server.on("/v1/status", HTTP_GET, [] {
        reply(200, warmedUp()
            ? "{\"version\":1,\"source\":\"xiao-camera\",\"ready\":true}"
            : "{\"version\":1,\"source\":\"xiao-camera\",\"ready\":false}");
    });
    server.on("/v1/scan", HTTP_POST, scan);
    server.on("/v1/listen", HTTP_POST, [] {
        char json[384];
        const auto status = listen(json, sizeof(json));
        reply(status, json);
    });
    server.onNotFound([] { reply(404, "{\"error\":\"not_found\"}"); });
    server.begin();
    Serial.printf("Join Wi-Fi %s, password %s; toy address http://%s\n",
                  kAccessPoint, kPassword, WiFi.softAPIP().toString().c_str());
}

void loop() {
    if (Serial.available()) {
        const char command = Serial.read();
        if (command == 'a') {
            char json[384];
            listen(json, sizeof(json));
            Serial.println(json);
        } else if (command == 's') {
            char json[256];
            capture(json, sizeof(json));
            Serial.println(json);
        } else if (command == 'p') {
            if (!warmedUp()) Serial.println("{\"error\":\"camera_not_ready\"}");
            else {
                { Frame stale(esp_camera_fb_get()); }
                Frame frame(esp_camera_fb_get());
                if (!frame) Serial.println("{\"error\":\"capture_failed\"}");
                else {
                    Serial.printf("JPEG %u\n", static_cast<unsigned>(frame->len));
                    Serial.write(frame->buf, frame->len);
                }
            }
        }
    }
    server.handleClient();
    delay(2); // Yield to networking/watchdog tasks without a busy loop.
}
