#pragma once

#include <cstddef>
#include <cstdint>

namespace ardventure {
struct ColourReading {
    std::uint8_t red = 0;
    std::uint8_t green = 0;
    std::uint8_t blue = 0;
    std::uint32_t samples = 0;
};

// No Arduino dependencies: independently testable on a desktop compiler.
// esp32-camera's fmt2rgb888 output is stored in B, G, R byte order.
inline bool sampleCentreBgr(const std::uint8_t* pixels, std::size_t bytes,
                            std::size_t width, std::size_t height, ColourReading& result) {
    result = {};
    if (!pixels || width < 4 || height < 4 || width > 320 || height > 240) return false;
    if (bytes < width * height * 3) return false;
    std::uint32_t red = 0, green = 0, blue = 0, count = 0;
    // Central half of each axis: aim a plain coloured object at the camera centre.
    for (std::size_t y = height / 4; y < height * 3 / 4; ++y) {
        for (std::size_t x = width / 4; x < width * 3 / 4; ++x) {
            const auto offset = (y * width + x) * 3;
            blue += pixels[offset];
            green += pixels[offset + 1];
            red += pixels[offset + 2];
            ++count;
        }
    }
    result = {static_cast<std::uint8_t>(red / count),
              static_cast<std::uint8_t>(green / count),
              static_cast<std::uint8_t>(blue / count), count};
    return true;
}
} // namespace ardventure
