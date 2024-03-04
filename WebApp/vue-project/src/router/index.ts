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
            path: '/PrivacyPolicy',
            name: 'PrivacyPolicy',
            component: () => import('../views/PrivacyPolicy.vue')
        },
        {
            path: '/Contacts',
            name: 'Contacts',
            component: () => import('../views/Contacts.vue')
        },
        {
            path: '/UserInfo',
            name: 'UserInfo',
            component: () => import('../views/UserInfo.vue')
        },
    ]
})

export default router