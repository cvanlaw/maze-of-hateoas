---
id: 1
title: Direction Enum
depends_on: []
status: pending
---

# Task 1: Direction Enum

## Description

Create the Direction enum representing the four cardinal directions in the maze. This is the foundational type used by Position and Cell. Following TDD, write tests first to define the expected enum values.

## Deliverables

- `src/MazeOfHateoas.Domain/Direction.cs` - Direction enum definition
- `tests/MazeOfHateoas.Domain.Tests/DirectionTests.cs` - Unit tests for Direction enum

## Acceptance Criteria

- [ ] Tests written first that verify Direction enum has exactly four values
- [ ] Direction enum exists with values: `North`, `South`, `East`, `West`
- [ ] Enum values are defined (no explicit integer assignments needed)
- [ ] All tests pass via `docker compose -f docker-compose.test.yml up --build`

## Implementation Details

### Test First (TDD)

```csharp
public class DirectionTests
{
    [Fact]
    public void Direction_HasFourValues()
    {
        var values = Enum.GetValues<Direction>();
        Assert.Equal(4, values.Length);
    }

    [Theory]
    [InlineData(Direction.North)]
    [InlineData(Direction.South)]
    [InlineData(Direction.East)]
    [InlineData(Direction.West)]
    public void Direction_ContainsExpectedValue(Direction direction)
    {
        Assert.True(Enum.IsDefined(direction));
    }
}
```

### Implementation

```csharp
namespace MazeOfHateoas.Domain;

public enum Direction
{
    North,
    South,
    East,
    West
}
```

## Testing Checklist

- [ ] Write tests before implementation (red phase)
- [ ] Run tests in Docker - should fail (no Direction yet)
- [ ] Implement Direction enum (green phase)
- [ ] Run tests in Docker - should pass
- [ ] Verify: `docker compose -f docker-compose.test.yml up --build`
