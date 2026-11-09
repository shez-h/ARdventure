#include "../ardventure_toy/SoundMeter.h"
#include <array>
#include <cassert>
#include <cmath>
#include <iostream>

int main() {
    std::array<std::int16_t, 320> block{};
    ardventure::SoundMeter silent;
    assert(!silent.add(nullptr, 320) && !silent.add(block.data(), 0) && !silent.add(block.data(), 321));
    assert(silent.add(block.data(), block.size()));
    assert(silent.rms() == 0 && silent.peak() == 0);
    block.fill(12000);
    ardventure::SoundMeter offset;
    assert(offset.add(block.data(), block.size()));
    assert(offset.rms() == 0); // Constant DC is not audible energy.
    for (std::size_t i = 0; i < block.size(); ++i) block[i] = i % 2 ? 4000 : -4000;
    ardventure::SoundMeter tone;
    assert(tone.add(block.data(), block.size()));
    assert(std::abs(tone.rms() - 4000.0 / 32768) < 1e-9);
    block.fill(0);
    assert(tone.add(block.data(), block.size()));
    assert(std::abs(tone.rms() - 4000.0 / 32768 / std::sqrt(2.0)) < 1e-9);
    assert(std::abs(tone.peak() - 4000.0 / 32768) < 1e-9); // A transient survives the aggregate.
    for (std::size_t i = 0; i < block.size(); ++i) block[i] = i % 2 ? 32767 : -32768;
    ardventure::SoundMeter clipped;
    assert(clipped.add(block.data(), block.size()));
    assert(clipped.clippedFraction() == 1 && clipped.rms() <= 1 && clipped.rms() > 0.99);
    ardventure::SoundMeter bounded;
    for (int i = 0; i < 100; ++i) assert(bounded.add(block.data(), block.size()));
    assert(bounded.samples() == 32000 && !bounded.add(block.data(), 1));
    std::cout << "Sound meter checks passed (silence, DC bias, RMS, transient, clipping, bounds).\n";
}
