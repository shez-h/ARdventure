#include "../ardventure_toy/ColourSampler.h"
#include <array>
#include <cassert>
#include <iostream>
#include <vector>

int main() {
    ardventure::ColourReading reading;
    assert(!ardventure::sampleCentreBgr(nullptr, 0, 320, 240, reading));
    std::array<std::uint8_t, 4 * 4 * 3> pixels{};
    assert(!ardventure::sampleCentreBgr(pixels.data(), pixels.size() - 1, 4, 4, reading));
    assert(!ardventure::sampleCentreBgr(pixels.data(), pixels.size(), 0, 4, reading));
    assert(!ardventure::sampleCentreBgr(pixels.data(), pixels.size(), 321, 240, reading));
    // Blue outside the centre must not contaminate the green object in the middle.
    for (std::size_t i = 0; i < pixels.size(); i += 3) pixels[i] = 255;
    for (std::size_t y = 1; y < 3; ++y) for (std::size_t x = 1; x < 3; ++x) {
        const auto i = (y * 4 + x) * 3;
        pixels[i] = 30; pixels[i + 1] = 200; pixels[i + 2] = 10;
    }
    assert(ardventure::sampleCentreBgr(pixels.data(), pixels.size(), 4, 4, reading));
    assert(reading.red == 10 && reading.green == 200 && reading.blue == 30 && reading.samples == 4);
    std::vector<std::uint8_t> white(320 * 240 * 3, 255);
    assert(ardventure::sampleCentreBgr(white.data(), white.size(), 320, 240, reading));
    assert(reading.red == 255 && reading.green == 255 && reading.blue == 255 && reading.samples == 19200);
    white.assign(white.size(), 0);
    assert(ardventure::sampleCentreBgr(white.data(), white.size(), 320, 240, reading));
    assert(reading.red == 0 && reading.green == 0 && reading.blue == 0);
    std::cout << "Colour sampler checks passed (bounds, channel order, ROI, white, black).\n";
}
