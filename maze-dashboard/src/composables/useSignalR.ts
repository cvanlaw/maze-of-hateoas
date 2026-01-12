import { ref, onUnmounted } from 'vue';
import * as signalR from '@microsoft/signalr';
import type {
  SessionStartedEvent,
  SessionMovedEvent,
  SessionCompletedEvent
} from '../types';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:8080';

export function useSignalR() {
  const connection = ref<signalR.HubConnection | null>(null);
  const isConnected = ref(false);
  const error = ref<string | null>(null);

  const onSessionStarted = ref<((event: SessionStartedEvent) => void) | null>(null);
  const onSessionMoved = ref<((event: SessionMovedEvent) => void) | null>(null);
  const onSessionCompleted = ref<((event: SessionCompletedEvent) => void) | null>(null);

  async function connect() {
    try {
      connection.value = new signalR.HubConnectionBuilder()
        .withUrl(`${API_URL}/hubs/metrics`)
        .withAutomaticReconnect()
        .build();

      connection.value.on('SessionStarted', (event: SessionStartedEvent) => {
        onSessionStarted.value?.(event);
      });

      connection.value.on('SessionMoved', (event: SessionMovedEvent) => {
        onSessionMoved.value?.(event);
      });

      connection.value.on('SessionCompleted', (event: SessionCompletedEvent) => {
        onSessionCompleted.value?.(event);
      });

      await connection.value.start();
      isConnected.value = true;
      error.value = null;
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Connection failed';
      isConnected.value = false;
    }
  }

  async function subscribeToAll() {
    if (connection.value?.state === signalR.HubConnectionState.Connected) {
      await connection.value.invoke('SubscribeToAll');
    }
  }

  async function subscribeToMaze(mazeId: string) {
    if (connection.value?.state === signalR.HubConnectionState.Connected) {
      await connection.value.invoke('SubscribeToMaze', mazeId);
    }
  }

  async function unsubscribeFromMaze(mazeId: string) {
    if (connection.value?.state === signalR.HubConnectionState.Connected) {
      await connection.value.invoke('UnsubscribeFromMaze', mazeId);
    }
  }

  async function disconnect() {
    if (connection.value) {
      await connection.value.stop();
      isConnected.value = false;
    }
  }

  onUnmounted(() => {
    disconnect();
  });

  return {
    isConnected,
    error,
    connect,
    disconnect,
    subscribeToAll,
    subscribeToMaze,
    unsubscribeFromMaze,
    onSessionStarted,
    onSessionMoved,
    onSessionCompleted
  };
}
