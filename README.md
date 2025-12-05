# LexoRank

[![NuGet](https://img.shields.io/nuget/v/LexoRank.svg)](https://www.nuget.org/packages/Innoloft.LexoRank/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/)

A .NET implementation of the Atlassian LexoRank algorithm. LexoRank is a ranking system that allows for efficient reordering of items in a list without requiring updates to other items.

## Features

*   **Core LexoRank Logic**: Full implementation of buckets, base36 arithmetic, and rank generation.
*   **Timestamp Conversion**: Convert timestamps to LexoRank values to preserve chronological order.
*   **String-based Calculation**: Helper methods to calculate ranks between two strings.
*   **Thread-Safe**: Immutable design ensures safe concurrent access.
*   **Robust**: Handles edge cases and ensures correct rank distribution.

## How It Works

LexoRank is a ranking system that uses lexicographically ordered strings to maintain item positions. Unlike traditional integer-based ranking systems, LexoRank allows you to insert items between any two existing items without updating other items in the list.

### Key Concepts

**Base36 Encoding**: LexoRank uses base36 (0-9, a-z) to represent rank values, providing a large namespace for positions.

**Buckets**: The system uses three buckets (0, 1, 2) to manage rebalancing. When a bucket becomes too dense with items, you can move items to another bucket to maintain spacing.

**Between Operation**: The core operation calculates a rank between two existing ranks by finding the lexicographic midpoint.

### Example

```csharp
var rank1 = LexoRank.Min();           // "0|000000:"
var rank2 = LexoRank.Max();           // "0|zzzzzz:"
var middle = rank1.Between(rank2);    // "0|hzzzzz:"
var next = middle.GenNext();          // "0|i00007:"
```

## Performance Characteristics

- **Insert Between**: O(1) - No other items need to be updated
- **Generate Next/Previous**: O(1) - Constant time operation
- **String Comparison**: O(n) where n is the length of the rank string (typically 8-10 characters)
- **Memory**: Each rank is approximately 8-10 bytes as a string
- **Thread Safety**: All operations are thread-safe due to immutable design

### When to Rebalance

While LexoRank minimizes updates, you may need to rebalance when:
- Ranks become very long (>20 characters) due to many insertions
- A bucket becomes too dense
- You want to optimize for shorter rank strings

Rebalancing involves moving items to a different bucket or regenerating ranks with better spacing.

## Installation

Install the package via NuGet:

```bash
dotnet add package Innoloft.LexoRank
```

## Usage

### Basic Usage

```csharp
using LexoRank;

// Get the minimum and maximum ranks
var min = LexoRank.Min();
var max = LexoRank.Max();

// Get the middle rank
var middle = LexoRank.Middle();

// Generate the next rank
var next = middle.GenNext();

// Generate the previous rank
var prev = middle.GenPrev();

// Calculate a rank between two ranks
var between = min.Between(max);
```

### Timestamp to LexoRank

You can convert a `DateTime` to a `LexoRank`. This is useful for initializing ranks based on creation time.

```csharp
var now = DateTime.UtcNow;
var rank = LexoRank.FromTimestamp(now);
```

### String-based Calculation

If you are storing ranks as strings and want to calculate a new rank between two existing rank strings:

```csharp
string prevRank = "0|000000:";
string nextRank = "0|zzzzzz:";

// Calculate rank between prev and next
var newRank = LexoRank.CalculateBetween(prevRank, nextRank);

// Calculate rank at the beginning (before nextRank)
var startRank = LexoRank.CalculateBetween(null, nextRank);

// Calculate rank at the end (after prevRank)
var endRank = LexoRank.CalculateBetween(prevRank, null);
```

## Buckets

LexoRank uses buckets to manage rebalancing. This implementation supports the standard 3 buckets (0, 1, 2).

```csharp
// Get the bucket of a rank
var bucket = rank.Bucket;

// Move to the next bucket
var nextBucketRank = rank.InNextBucket();
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

MIT