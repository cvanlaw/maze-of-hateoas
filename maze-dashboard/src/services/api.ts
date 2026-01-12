import type { AggregateMetrics, MazeMetrics, MazeSummary } from '../types';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:8080';

export async function fetchAggregateMetrics(): Promise<AggregateMetrics> {
  const response = await fetch(`${API_URL}/api/metrics`);
  if (!response.ok) throw new Error('Failed to fetch aggregate metrics');
  return response.json();
}

export async function fetchMazeMetrics(mazeId: string): Promise<MazeMetrics> {
  const response = await fetch(`${API_URL}/api/metrics/mazes/${mazeId}`);
  if (!response.ok) throw new Error('Failed to fetch maze metrics');
  return response.json();
}

export async function fetchMazes(): Promise<MazeSummary[]> {
  const response = await fetch(`${API_URL}/api/mazes`);
  if (!response.ok) throw new Error('Failed to fetch mazes');
  const data = await response.json();
  return data.mazes || [];
}
