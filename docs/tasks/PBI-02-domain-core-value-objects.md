# PBI-02: Domain Core Value Objects

## Status: Ready

## Description

Implement the core domain value objects that represent positions, directions, and cells in the maze. These are pure C# types with no external dependencies, forming the vocabulary for all maze-related logic. All behavior is test-driven.

## Acceptance Criteria

- [ ] `Direction` enum with values: `North`, `South`, `East`, `West`
- [ ] `Position` readonly record struct with:
  - [ ] Properties: `X`, `Y` (integers)
  - [ ] Method: `Move(Direction direction)` returns new Position offset by direction
  - [ ] North decreases Y, South increases Y, East increases X, West decreases X
- [ ] `Cell` readonly record struct with:
  - [ ] Property: `Position`
  - [ ] Properties: `HasNorthWall`, `HasSouthWall`, `HasEastWall`, `HasWestWall` (booleans)
  - [ ] Method: `CanMove(Direction direction)` returns true if no wall blocks that direction
- [ ] Unit tests verify all `Position.Move()` direction calculations
- [ ] Unit tests verify all `Cell.CanMove()` wall-blocking logic
- [ ] All tests pass when run via `docker compose -f docker-compose.test.yml up --build`

## Dependencies

- PBI-01 (Docker Test Infrastructure)

## Technical Notes

### TDD Order
1. Write tests for `Direction` enum existence
2. Write tests for `Position.Move()` - all 4 directions
3. Write tests for `Cell.CanMove()` - all wall combinations
4. Implement each to make tests pass

### Position Coordinate System
```
        North (Y-1)
           ↑
West (X-1) ← → East (X+1)
           ↓
        South (Y+1)
```

### Example Tests
```csharp
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
```

## Verification

```bash
docker compose -f docker-compose.test.yml up --build
# All Position and Cell tests should pass
```
