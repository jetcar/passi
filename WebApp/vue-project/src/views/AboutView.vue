<template>
    <div class="about-page">
        <div class="container py-5">
            <div class="row justify-content-center">
                <div class="col-lg-10">
                    <div class="text-center mb-5">
                        <h1 class="display-4 fw-bold mb-3">{{ $t('about.title') }}</h1>
                        <p class="lead text-muted">{{ $t('about.role') }}</p>
                        <p class="text-muted">
                            <i class="bi bi-envelope me-2"></i>admin@passi.cloud
                        </p>
                    </div>

                    <div class="card shadow-sm mb-4">
                        <div class="card-body p-5">
                            <h2 class="h3 mb-4">
                                <i class="bi bi-person text-primary me-2"></i>
                                {{ $t('about.professionalTitle') }}
                            </h2>
                            <p class="lead">
                                {{ $t('about.professional1') }}
                            </p>
                            <p>
                                {{ $t('about.professional2') }}
                            </p>
                            <p>
                                {{ $t('about.professional3') }}
                            </p>
                            <p class="mb-0">
                                {{ $t('about.professional4') }}
                            </p>
                        </div>
                    </div>

                    <div class="card shadow-sm mb-4">
                        <div class="card-body p-5">
                            <h2 class="h3 mb-4">
                                <i class="bi bi-code-slash text-primary me-2"></i>
                                {{ $t('about.expertiseTitle') }}
                            </h2>
                            <p>
                                {{ $t('about.expertiseIntro') }}
                            </p>
                            <ul class="feature-list">
                                <li v-for="key in expertiseKeys" :key="key">{{ $t(key) }}</li>
                            </ul>
                        </div>
                    </div>

                    <div class="card shadow-sm mb-4">
                        <div class="card-body p-5">
                            <h2 class="h3 mb-4">
                                <i class="bi bi-rocket text-primary me-2"></i>
                                {{ $t('about.projectsTitle') }}
                            </h2>
                            <p>
                                {{ $t('about.projectsIntro') }}
                            </p>
                            <ul class="feature-list">
                                <li v-for="key in projectKeys" :key="key">{{ $t(key) }}</li>
                            </ul>
                            <p class="mb-0">
                                {{ $t('about.projectsOutro') }}
                            </p>
                        </div>
                    </div>

                    <div class="card shadow-sm mb-4 bg-light">
                        <div class="card-body p-5">
                            <h2 class="h3 mb-4">
                                <i class="bi bi-chat-quote text-primary me-2"></i>
                                {{ $t('about.whyTitle') }}
                            </h2>
                            <blockquote class="blockquote mb-0">
                                <p class="fs-5">
                                    “{{ $t('about.quote') }}”
                                </p>
                            </blockquote>
                            <p class="mt-4 mb-0">
                                {{ $t('about.whyText') }}
                            </p>
                        </div>
                    </div>

                    <div class="text-center mt-5">
                        <h3 class="mb-4">{{ $t('about.connect') }}</h3>
                        <div class="d-flex justify-content-center gap-3 flex-wrap">
                            <a href="https://github.com/jetcar/passi" target="_blank" 
                               class="btn btn-outline-dark btn-lg">
                                <i class="bi bi-github me-2"></i>{{ $t('common.viewGithub') }}
                            </a>
                            <a href="https://www.youtube.com/watch?v=Koe_8YcmYkU" target="_blank" 
                               class="btn btn-outline-danger btn-lg">
                                <i class="bi bi-youtube me-2"></i>{{ $t('common.watchDemo') }}
                            </a>
                            <router-link to="/Contacts" class="btn btn-outline-primary btn-lg">
                                <i class="bi bi-envelope me-2"></i>{{ $t('about.getInTouch') }}
                            </router-link>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
const expertiseKeys = Array.from({ length: 11 }, (_, index) => `about.expertise${index + 1}`)
const projectKeys = Array.from({ length: 6 }, (_, index) => `about.project${index + 1}`)
</script>

<style scoped>
.about-page {
    background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
    min-height: calc(100vh - 200px);
}

.card {
    border: none;
    border-radius: 15px;
    transition: transform 0.3s ease, box-shadow 0.3s ease;
}

.card:hover {
    transform: translateY(-5px);
    box-shadow: 0 10px 30px rgba(0, 0, 0, 0.15) !important;
}

.feature-list {
    list-style: none;
    padding-left: 0;
}

.feature-list li {
    padding: 0.5rem 0;
    padding-left: 1.5rem;
    position: relative;
}

.feature-list li:before {
    content: "✓";
    position: absolute;
    left: 0;
    color: #0d6efd;
    font-weight: bold;
}

blockquote {
    border-left: 4px solid #0d6efd;
    padding-left: 1.5rem;
}

.btn-lg {
    padding: 0.75rem 1.5rem;
    font-size: 1.1rem;
}
</style>
