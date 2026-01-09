---
id: 2
title: Position Value Object
depends_on: [1]
status: pending
---

# Task 2: Position Value Object

## Description

Create the Position readonly record struct representing a coordinate in the maze grid. The Move method calculates adjacent positions based on direction. Following TDD, write comprehensive tests for all four directional movements before implementation.

## Deliverables

- `src/MazeOfHateoas.Domain/Position.cs` - Position readonly record struct
- `tests/MazeOfHateoas.Domain.Tests/PositionTests.cs` - Unit tests for Position

## Acceptance Criteria

- [ ] Tests written first for Position construction and Move method
- [ ] Position is a `readonly record struct` with `X` and `Y` integer properties
- [ ] `Move(Direction direction)` returns new Position offset by direction
- [ ] North decreases Y (Move(North) from (5,5) = (5,4))
- [ ] South increases Y (Move(South) from (5,5) = (5,6))
- [ ] East increases X (Move(East) from (5,5) = (6,5))
- [ ] West decreases X (Move(West) from (5,5) = (4,5))
- [ ] All tests pass via `docker compose -f docker-compose.test.yml up --build`

## Implementation Details

### Coordinate System

```
        North (Y-1)
           ^
West (X-1) < > East (X+1)
           v
        South (Y+1)
```

### Test First (TDD)

```csharp
public class PositionTests
{
    [Fact]
    public void Position_StoresCoordinates()
    {
        var position = new Position(3, 7);
        Assert.Equal(3, position.X);
        Assert.Equal(7, position.Y);
    }

    [Theory]
    [InlineData(Direction.North, 5, 4)]
    [InlineData(Direction.South, 5, 6)]
    [InlineData(Direction.East, 6, 5)]
    [InlineData(Direction.West, 4, 5)]
    public void Move_ReturnsCorrectPosition(Direction dir, int expectedX, int expectedY)
    {
        var position = new Position(5, 5);
        var result = position.Move(dir);
        Assert.Equal(new Position(expectedX, expectedY), result);
    }

    [Fact]
    public void Move_DoesNotMutateOriginal()
    {
        var original = new Position(5, 5);
        var moved = original.Move(Direction.North);
        Assert.Equal(new Position(5, 5), original);
        Assert.NotEqual(original, moved);
    }
}
```

### Implementation

```csharp
namespace MazeOfHateoas.Domain;

public readonly record struct Position(int X, int Y)
{
    public Position Move(Direction direction) => direction switch
    {
        Direction.North => this with { Y = Y - 1 },
        Direction.South => this with { Y = Y + 1 },
        Direction.East => this with { X = X + 1 },
        Direction.West => this with { X = X - 1 },
        _ => throw new ArgumentOutOfRangeException(nameof(direction))
    };
}
```

## Testing Checklist

- [ ] Write tests before implementation (red phase)
- [ ] Run tests in Docker - should fail (no Position yet)
- [ ] Implement Position record struct (green phase)
- [ ] Run tests in Docker - should pass
- [ ] Verify all 4 directions work correctly
- [ ] Verify: `docker compose -f docker-compose.test.yml up --build`
