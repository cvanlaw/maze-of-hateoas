export interface AggregateMetrics {
  activeSessions: number;
  completedToday: number;
  completionRate: number;
  averageMoves: number;
  mostActiveMazeId: string | null;
  mostActiveMazeSessionCount: number;
  systemVelocity: number;
  sessionCountsByMaze: Record<string, number>;
}

export interface Position {
  x: number;
  y: number;
}

export interface SessionSnapshot {
  sessionId: string;
  currentPosition: Position;
  moveCount: number;
  visitedCount: number;
  completionPercent: number;
  velocity: number;
  duration: string;
}

export interface Cell {
  position: Position;
  hasNorthWall: boolean;
  hasSouthWall: boolean;
  hasEastWall: boolean;
  hasWestWall: boolean;
}

export interface MazeMetrics {
  mazeId: string;
  width: number;
  height: number;
  cells: Cell[][];
  activeSessions: number;
  totalCompleted: number;
  sessions: SessionSnapshot[];
}

export interface MazeSummary {
  id: string;
  width: number;
  height: number;
  createdAt: string;
}

export interface SessionStartedEvent {
  sessionId: string;
  mazeId: string;
  timestamp: string;
}

export interface SessionMovedEvent {
  sessionId: string;
  mazeId: string;
  positionX: number;
  positionY: number;
  moveCount: number;
  visitedCount: number;
}

export interface SessionCompletedEvent {
  sessionId: string;
  mazeId: string;
  moveCount: number;
  duration: string;
}
