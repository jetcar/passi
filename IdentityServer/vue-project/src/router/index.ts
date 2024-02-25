import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes: [
        {
            path: '/',
            name: 'Home',
            component: () => import('../views/HomeView.vue')
        },
        {
            path: '/ClientList',
            name: 'ClientList',
            component: () => import('../views/ClientList.vue')
        },
        {
            path: '/CreateClient',
            name: 'CreateClient',
            component: () => import('../views/CreateClient.vue')
        },
        {
            path: '/ClientDetails/:id',
            name: 'ClientDetails',
            component: () => import('../views/ClientDetails.vue')
        },
        {
            path: '/login',
            name: 'Login',
            component: () => import('../views/LoginView.vue')
        },
        {
            path: '/Account/Login',
            name: 'Logn',
            component: () => import('../views/LoginView.vue')
        },
        {
            path: '/DeleteUser',
            name: 'Logn',
            component: () => import('../views/DeleteUserView.vue')
        },
    ]
})

export default router