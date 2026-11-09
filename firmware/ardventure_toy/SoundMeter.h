#pragma once
#include <cmath>
#include <cstddef>
#include <cstdint>

namespace ardventure {
// Streaming 20 ms RMS windows. Raw PCM is discarded after each block.
// Removing each block's DC offset prevents microphone bias from becoming "sound".
class SoundMeter {
public:
    bool add(const std::int16_t* pcm, std::size_t count) {
        if (!pcm || count == 0 || count > 320 || samples_ + count > 32000) return false;
        std::int64_t sum = 0;
        std::uint64_t squares = 0;
        for (std::size_t i = 0; i < count; ++i) {
            const std::int32_t value = pcm[i];
            sum += value;
            squares += static_cast<std::uint64_t>(static_cast<std::int64_t>(value) * value);
            if (value >= 32760 || value <= -32760) ++clipped_;
        }
        const double mean = static_cast<double>(sum) / count;
        const double variance = std::fmax(0.0, static_cast<double>(squares) / count - mean * mean);
        energy_ += variance * count;
        samples_ += count;
        const double rms = std::sqrt(variance) / 32768.0;
        peak_ = std::fmax(peak_, rms);
        return true;
    }
    std::uint32_t samples() const { return samples_; }
    double rms() const { return samples_ ? std::sqrt(energy_ / samples_) / 32768.0 : 0; }
    double peak() const { return peak_; }
    double clippedFraction() const { return samples_ ? static_cast<double>(clipped_) / samples_ : 0; }
private:
    double energy_ = 0, peak_ = 0;
    std::uint32_t samples_ = 0, clipped_ = 0;
};
} // namespace ardventure
