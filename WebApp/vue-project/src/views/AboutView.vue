<template>
    <div class="about-page">
        <div class="container py-5">
            <div class="row justify-content-center">
                <div class="col-lg-10">
                    <div class="text-center mb-5">
                        <h1 class="display-4 fw-bold mb-3">About This Project</h1>
                        <p class="lead text-muted">Building secure authentication solutions for the modern web</p>
                    </div>

                    <div class="card shadow-sm mb-4">
                        <div class="card-body p-5">
                            <h2 class="h3 mb-4">
                                <i class="bi bi-lightbulb text-primary me-2"></i>
                                The Vision
                            </h2>
                            <p class="lead">
                                Passi was born from a simple observation: passwords are fundamentally broken. They're hard to remember, 
                                easy to steal, and create friction in the user experience. There had to be a better way.
                            </p>
                            <p>
                                This project explores passwordless authentication using public-key cryptography and OAuth2 standards. 
                                By combining certificate-based authentication with modern web protocols, we've created a system that's 
                                both more secure and more convenient than traditional passwords.
                            </p>
                        </div>
                    </div>

                    <div class="card shadow-sm mb-4">
                        <div class="card-body p-5">
                            <h2 class="h3 mb-4">
                                <i class="bi bi-gear text-primary me-2"></i>
                                The Technology
                            </h2>
                            <p>
                                Passi is built on proven cryptographic principles and open standards:
                            </p>
                            <ul class="feature-list">
                                <li><strong>Public Key Infrastructure (PKI):</strong> Using X.509 certificates for authentication</li>
                                <li><strong>OAuth2 & OpenID Connect:</strong> Standard protocols for secure authorization</li>
                                <li><strong>Modern Web Stack:</strong> ASP.NET Core, Vue.js, Redis, and PostgreSQL</li>
                                <li><strong>Mobile First:</strong> Native Android app for seamless authentication on the go</li>
                                <li><strong>Open Source:</strong> Fully transparent and available on GitHub</li>
                            </ul>
                        </div>
                    </div>

                    <div class="card shadow-sm mb-4">
                        <div class="card-body p-5">
                            <h2 class="h3 mb-4">
                                <i class="bi bi-code-slash text-primary me-2"></i>
                                Open Source Philosophy
                            </h2>
                            <p>
                                Security through obscurity doesn't work. That's why Passi is completely open source. 
                                Anyone can review the code, audit the security, and contribute improvements.
                            </p>
                            <p class="mb-0">
                                The project is hosted on <a href="https://github.com/jetcar/passi" target="_blank" class="text-decoration-none">
                                GitHub <i class="bi bi-box-arrow-up-right"></i></a>, where you can explore the implementation, 
                                report issues, or submit pull requests.
                            </p>
                        </div>
                    </div>

                    <div class="card shadow-sm mb-4">
                        <div class="card-body p-5">
                            <h2 class="h3 mb-4">
                                <i class="bi bi-rocket text-primary me-2"></i>
                                The Journey
                            </h2>
                            <p>
                                What started as an exploration of certificate-based authentication evolved into a full-featured 
                                passwordless authentication platform. Along the way, several challenges were solved:
                            </p>
                            <ul class="feature-list">
                                <li>How to make certificate authentication user-friendly on mobile devices</li>
                                <li>Integrating with existing OAuth2/OIDC ecosystems</li>
                                <li>Building a secure yet performant authentication flow</li>
                                <li>Creating developer-friendly APIs and documentation</li>
                            </ul>
                            <p class="mb-0">
                                Each challenge led to new insights and improvements, resulting in the system you see today.
                            </p>
                        </div>
                    </div>

                    <div class="card shadow-sm mb-4 bg-light">
                        <div class="card-body p-5">
                            <h2 class="h3 mb-4">
                                <i class="bi bi-chat-quote text-primary me-2"></i>
                                Why It Matters
                            </h2>
                            <blockquote class="blockquote mb-0">
                                <p class="fs-5">
                                    "Authentication should be invisible when it works, and unbreakable when it matters. 
                                    Passwords fail at both."
                                </p>
                            </blockquote>
                            <p class="mt-4 mb-0">
                                Passi demonstrates that we can do better. By removing passwords from the equation, we eliminate 
                                an entire class of security vulnerabilities while simultaneously improving the user experience.
                            </p>
                        </div>
                    </div>

                    <div class="text-center mt-5">
                        <h3 class="mb-4">Get Involved</h3>
                        <div class="d-flex justify-content-center gap-3 flex-wrap">
                            <a href="https://github.com/jetcar/passi" target="_blank" 
                               class="btn btn-outline-dark btn-lg">
                                <i class="bi bi-github me-2"></i>View on GitHub
                            </a>
                            <a href="https://www.youtube.com/watch?v=tRrWp6LWQNU" target="_blank" 
                               class="btn btn-outline-danger btn-lg">
                                <i class="bi bi-youtube me-2"></i>Watch Demo
                            </a>
                            <router-link to="/Contacts" class="btn btn-outline-primary btn-lg">
                                <i class="bi bi-envelope me-2"></i>Get in Touch
                            </router-link>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
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
