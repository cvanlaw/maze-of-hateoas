---
id: 3
title: Cell Value Object
depends_on: [1, 2]
status: pending
---

# Task 3: Cell Value Object

## Description

Create the Cell readonly record struct representing a single cell in the maze grid. Each cell has a position and four walls. The CanMove method determines if movement in a given direction is blocked by a wall. Following TDD, write tests covering all wall combinations before implementation.

## Deliverables

- `src/MazeOfHateoas.Domain/Cell.cs` - Cell readonly record struct
- `tests/MazeOfHateoas.Domain.Tests/CellTests.cs` - Unit tests for Cell

## Acceptance Criteria

- [ ] Tests written first for Cell construction and CanMove method
- [ ] Cell is a `readonly record struct` with Position property
- [ ] Cell has boolean wall properties: `HasNorthWall`, `HasSouthWall`, `HasEastWall`, `HasWestWall`
- [ ] `CanMove(Direction direction)` returns true if no wall blocks that direction
- [ ] CanMove(North) returns `!HasNorthWall`
- [ ] CanMove(South) returns `!HasSouthWall`
- [ ] CanMove(East) returns `!HasEastWall`
- [ ] CanMove(West) returns `!HasWestWall`
- [ ] All tests pass via `docker compose -f docker-compose.test.yml up --build`

## Implementation Details

### Test First (TDD)

```csharp
public class CellTests
{
    [Fact]
    public void Cell_StoresPosition()
    {
        var position = new Position(2, 3);
        var cell = new Cell(position, true, false, true, false);
        Assert.Equal(position, cell.Position);
    }

    [Fact]
    public void Cell_StoresWalls()
    {
        var cell = new Cell(new Position(0, 0),
            HasNorthWall: true,
            HasSouthWall: false,
            HasEastWall: true,
            HasWestWall: false);

        Assert.True(cell.HasNorthWall);
        Assert.False(cell.HasSouthWall);
        Assert.True(cell.HasEastWall);
        Assert.False(cell.HasWestWall);
    }

    [Theory]
    [InlineData(Direction.North, false, true)]   // No north wall = can move north
    [InlineData(Direction.North, true, false)]   // Has north wall = cannot move north
    [InlineData(Direction.South, false, true)]
    [InlineData(Direction.South, true, false)]
    [InlineData(Direction.East, false, true)]
    [InlineData(Direction.East, true, false)]
    [InlineData(Direction.West, false, true)]
    [InlineData(Direction.West, true, false)]
    public void CanMove_RespectsWalls(Direction direction, bool hasWall, bool expectedCanMove)
    {
        var cell = direction switch
        {
            Direction.North => new Cell(new Position(0, 0), HasNorthWall: hasWall, HasSouthWall: false, HasEastWall: false, HasWestWall: false),
            Direction.South => new Cell(new Position(0, 0), HasNorthWall: false, HasSouthWall: hasWall, HasEastWall: false, HasWestWall: false),
            Direction.East => new Cell(new Position(0, 0), HasNorthWall: false, HasSouthWall: false, HasEastWall: hasWall, HasWestWall: false),
            Direction.West => new Cell(new Position(0, 0), HasNorthWall: false, HasSouthWall: false, HasEastWall: false, HasWestWall: hasWall),
            _ => throw new ArgumentOutOfRangeException()
        };

        Assert.Equal(expectedCanMove, cell.CanMove(direction));
    }

    [Fact]
    public void CanMove_AllWalls_NoMovementPossible()
    {
        var cell = new Cell(new Position(0, 0), true, true, true, true);

        Assert.False(cell.CanMove(Direction.North));
        Assert.False(cell.CanMove(Direction.South));
        Assert.False(cell.CanMove(Direction.East));
        Assert.False(cell.CanMove(Direction.West));
    }

    [Fact]
    public void CanMove_NoWalls_AllMovementPossible()
    {
        var cell = new Cell(new Position(0, 0), false, false, false, false);

        Assert.True(cell.CanMove(Direction.North));
        Assert.True(cell.CanMove(Direction.South));
        Assert.True(cell.CanMove(Direction.East));
        Assert.True(cell.CanMove(Direction.West));
    }
}
```

### Implementation

```csharp
namespace MazeOfHateoas.Domain;

public readonly record struct Cell(
    Position Position,
    bool HasNorthWall,
    bool HasSouthWall,
    bool HasEastWall,
    bool HasWestWall)
{
    public bool CanMove(Direction direction) => direction switch
    {
        Direction.North => !HasNorthWall,
        Direction.South => !HasSouthWall,
        Direction.East => !HasEastWall,
        Direction.West => !HasWestWall,
        _ => throw new ArgumentOutOfRangeException(nameof(direction))
    };
}
```

## Testing Checklist

- [ ] Write tests before implementation (red phase)
- [ ] Run tests in Docker - should fail (no Cell yet)
- [ ] Implement Cell record struct (green phase)
- [ ] Run tests in Docker - should pass
- [ ] Verify all wall/direction combinations
- [ ] Verify: `docker compose -f docker-compose.test.yml up --build`
