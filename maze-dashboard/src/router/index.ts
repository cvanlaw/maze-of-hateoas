import { createRouter, createWebHistory } from 'vue-router';
import AggregateView from '../views/AggregateView.vue';
import MazeDetailView from '../views/MazeDetailView.vue';

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'home',
      component: AggregateView
    },
    {
      path: '/maze/:id',
      name: 'maze-detail',
      component: MazeDetailView
    }
  ]
});

export default router;
