<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import { ref, onMounted } from 'vue'

const API_URL = `/api/UserLoggedIn`

const isLoggedIn = ref(false)

const fetchData = async () => {
    try {
        const response = await fetch(API_URL)
        isLoggedIn.value = await response.json()
    } catch (error) {
        console.error('Error fetching user data:', error)
    }
}

onMounted(() => {
    fetchData()
})
</script>

<template>
    <div id="app">
        <header class="main-header">
            <nav class="navbar navbar-expand-lg navbar-dark bg-dark shadow-sm">
                <div class="container">
                    <router-link to="/" class="navbar-brand d-flex align-items-center">
                        <i class="bi bi-fingerprint me-2"></i>
                        <span class="fw-bold">Passi</span>
                    </router-link>
                    
                    <button class="navbar-toggler" type="button" data-bs-toggle="collapse" 
                            data-bs-target="#navbarNav" aria-controls="navbarNav" 
                            aria-expanded="false" aria-label="Toggle navigation">
                        <span class="navbar-toggler-icon"></span>
                    </button>
                    
                    <div class="collapse navbar-collapse" id="navbarNav">
                        <ul class="navbar-nav me-auto">
                            <li class="nav-item">
                                <router-link to="/" class="nav-link" active-class="active">
                                    <i class="bi bi-house-door me-1"></i>Home
                                </router-link>
                            </li>
                            <li class="nav-item dropdown">
                                <a class="nav-link dropdown-toggle" href="#" role="button" 
                                   data-bs-toggle="dropdown" aria-expanded="false">
                                    <i class="bi bi-box-seam me-1"></i>Products
                                </a>
                                <ul class="dropdown-menu">
                                    <li>
                                        <router-link to="/products" class="dropdown-item">
                                            <i class="bi bi-grid me-2"></i>All Products
                                        </router-link>
                                    </li>
                                    <li><hr class="dropdown-divider"></li>
                                    <li>
                                        <a href="/Home/DevTools" class="dropdown-item">
                                            <i class="bi bi-fingerprint me-2"></i>Passi Auth
                                        </a>
                                    </li>
                                    <li>
                                        <a href="https://github.com/jetcar/eid-openidc" target="_blank" class="dropdown-item">
                                            <i class="bi bi-credit-card me-2"></i>eID OpenIDC
                                        </a>
                                    </li>
                                </ul>
                            </li>
                            <li class="nav-item" v-if="isLoggedIn">
                                <router-link to="/UserInfo" class="nav-link">
                                    <i class="bi bi-person me-1"></i>User Info
                                </router-link>
                            </li>
                        </ul>
                        
                        <ul class="navbar-nav ms-auto">
                            <li class="nav-item">
                                <router-link to="/about" class="nav-link">
                                    <i class="bi bi-info-circle me-1"></i>About
                                </router-link>
                            </li>
                            <li class="nav-item">
                                <a href="/webmail" class="nav-link" target="_blank">
                                    <i class="bi bi-envelope-at me-1"></i>Webmail
                                </a>
                            </li>
                            <li class="nav-item">
                                <router-link to="/Contacts" class="nav-link">
                                    <i class="bi bi-envelope me-1"></i>Contact
                                </router-link>
                            </li>
                            <li class="nav-item">
                                <router-link to="/PrivacyPolicy" class="nav-link">
                                    <i class="bi bi-shield-lock me-1"></i>Privacy
                                </router-link>
                            </li>
                            <li class="nav-item" v-if="!isLoggedIn">
                                <a class="nav-link btn btn-outline-light btn-sm ms-2" href="/Auth/Login">
                                    <i class="bi bi-box-arrow-in-right me-1"></i>Login
                                </a>
                            </li>
                            <li class="nav-item" v-else>
                                <a class="nav-link btn btn-outline-light btn-sm ms-2" href="/Auth/Logout">
                                    <i class="bi bi-box-arrow-right me-1"></i>Logout
                                </a>
                            </li>
                        </ul>
                    </div>
                </div>
            </nav>
        </header>

        <main class="main-content">
            <RouterView />
        </main>

        <footer class="footer bg-dark text-white mt-5">
            <div class="container py-5">
                <div class="row">
                    <div class="col-md-4 mb-4">
                        <h5 class="mb-3">
                            <i class="bi bi-fingerprint me-2"></i>Passi
                        </h5>
                        <p class="text-muted">Passwordless authentication made simple. Secure, seamless, and built on open standards.</p>
                        <div class="social-links mt-3">
                            <a href="https://github.com/jetcar/passi" target="_blank" class="text-white me-3">
                                <i class="bi bi-github" style="font-size: 1.5rem;"></i>
                            </a>
                            <a href="https://www.youtube.com/watch?v=tRrWp6LWQNU" target="_blank" class="text-white">
                                <i class="bi bi-youtube" style="font-size: 1.5rem;"></i>
                            </a>
                        </div>
                    </div>
                    <div class="col-md-2 mb-4">
                        <h6 class="mb-3 text-white">Products</h6>
                        <ul class="list-unstyled">
                            <li class="mb-2"><router-link to="/products" class="text-light text-decoration-none hover-link">All Products</router-link></li>
                            <li class="mb-2"><a href="/Home/DevTools" class="text-light text-decoration-none hover-link">Passi Auth</a></li>
                            <li class="mb-2"><a href="https://github.com/jetcar/eid-openidc" target="_blank" class="text-light text-decoration-none hover-link">eID OpenIDC</a></li>
                        </ul>
                    </div>
                    <div class="col-md-3 mb-4">
                        <h6 class="mb-3 text-white">Links</h6>
                        <ul class="list-unstyled">
                            <li class="mb-2"><a href="https://github.com/jetcar/passi" target="_blank" class="text-light text-decoration-none hover-link">GitHub - Passi</a></li>
                            <li class="mb-2"><a href="https://www.youtube.com/watch?v=tRrWp6LWQNU" target="_blank" class="text-light text-decoration-none hover-link">Demo Video</a></li>
                            <li class="mb-2"><a href="/Home/DevTools" class="text-light text-decoration-none hover-link">Register Site</a></li>
                            <li class="mb-2"><router-link to="/Contacts" class="text-light text-decoration-none hover-link">Contact</router-link></li>
                            <li class="mb-2"><router-link to="/PrivacyPolicy" class="text-light text-decoration-none hover-link">Privacy Policy</router-link></li>
                        </ul>
                    </div>
                    <div class="col-md-3 mb-4">
                        <h6 class="mb-3 text-white">Download</h6>
                        <a href="https://play.google.com/store/apps/details?id=com.passi.cloud.passi_android" target="_blank">
                            <img src="/img/en_badge_web_generic.png" alt="Get it on Google Play" style="max-width: 150px">
                        </a>
                    </div>
                </div>
                <hr class="my-4 border-secondary">
                <div class="row">
                    <div class="col-md-6 text-center text-md-start">
                        <small class="text-muted">&copy; {{ new Date().getFullYear() }} Passi. Open source and transparent.</small>
                    </div>
                    <div class="col-md-6 text-center text-md-end">
                        <small class="text-muted">Built with Vue.js & OAuth2</small>
                    </div>
                </div>
            </div>
        </footer>
    </div>
</template>

<style>
@import 'bootstrap/dist/css/bootstrap.min.css';
@import 'bootstrap-icons/font/bootstrap-icons.css';

* {
    margin: 0;
    padding: 0;
    box-sizing: border-box;
}

body {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
    background-color: #f8f9fa;
}

#app {
    min-height: 100vh;
    display: flex;
    flex-direction: column;
}

.main-header {
    position: sticky;
    top: 0;
    z-index: 1000;
}

.navbar {
    padding: 0.75rem 1rem;
}

.navbar-brand {
    font-size: 1.5rem;
    transition: transform 0.3s ease;
}

.navbar-brand:hover {
    transform: scale(1.05);
}

.nav-link {
    transition: all 0.3s ease;
    border-radius: 5px;
    margin: 0 0.25rem;
    padding: 0.5rem 0.75rem;
}

.nav-link:hover {
    background-color: rgba(255, 255, 255, 0.1);
}

.nav-link.active {
    background-color: rgba(255, 255, 255, 0.2);
    font-weight: 600;
}

.main-content {
    flex: 1;
}

.footer {
    margin-top: auto;
}

.hover-link:hover {
    color: white !important;
}

.dropdown-menu {
    border: none;
    box-shadow: 0 5px 15px rgba(0, 0, 0, 0.2);
}

.dropdown-item {
    transition: all 0.3s ease;
    padding: 0.75rem 1.5rem;
}

.dropdown-item:hover {
    background-color: #667eea;
    color: white;
}

html {
    scroll-behavior: smooth;
}
</style>
