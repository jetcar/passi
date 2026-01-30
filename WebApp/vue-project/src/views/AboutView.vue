<template>
    <div class="about-page">
        <div class="container py-5">
            <div class="row justify-content-center">
                <div class="col-lg-10">
                    <div class="text-center mb-5">
                        <h1 class="display-4 fw-bold mb-3">About Me</h1>
                        <p class="lead text-muted">Solutions Architect & .NET Developer | Estonia</p>
                        <p class="text-muted">
                            <i class="bi bi-envelope me-2"></i>admin@passi.cloud
                        </p>
                    </div>

                    <div class="card shadow-sm mb-4">
                        <div class="card-body p-5">
                            <h2 class="h3 mb-4">
                                <i class="bi bi-person text-primary me-2"></i>
                                Professional Summary
                            </h2>
                            <p class="lead">
                                I am a highly experienced .NET developer and architect with over 15 years of expertise in designing, 
                                developing, and implementing scalable software solutions.
                            </p>
                            <p>
                                My career has been built on a deep understanding of the .NET ecosystem, from full-stack development 
                                to cloud-based architecture and microservices. I thrive on solving complex technical challenges and 
                                delivering innovative solutions that drive business success.
                            </p>
                            <p>
                                I leverage AI extensively in my development workflow, integrating AI agents and tools to maximize 
                                productivity and code quality. My approach combines traditional software engineering expertise with 
                                modern AI-assisted development practices.
                            </p>
                            <p class="mb-0">
                                With a strong background in software architecture, I excel in leading development teams, optimizing 
                                systems for performance, and ensuring seamless integration across diverse platforms. My passion for 
                                continuous learning keeps me up-to-date with the latest industry trends and technologies, allowing 
                                me to design forward-thinking, efficient systems.
                            </p>
                        </div>
                    </div>

                    <div class="card shadow-sm mb-4">
                        <div class="card-body p-5">
                            <h2 class="h3 mb-4">
                                <i class="bi bi-code-slash text-primary me-2"></i>
                                Technical Expertise
                            </h2>
                            <p>
                                Core competencies and technologies:
                            </p>
                            <ul class="feature-list">
                                <li><strong>.NET Ecosystem:</strong> ASP.NET Core, .NET Framework, C#</li>
                                <li><strong>Java:</strong> Java development, Spring Framework, Maven/Gradle</li>
                                <li><strong>AI & Automation:</strong> AI agent development, AI-assisted coding, GitHub Copilot integration</li>
                                <li><strong>Open Source:</strong> OpenID Connect (OIDC) implementations, OAuth2 authorization servers</li>
                                <li><strong>Architecture:</strong> Microservices, Cloud-based architecture, Scalable systems</li>
                                <li><strong>Databases:</strong> PostgreSQL, MS SQL, AuroraDB, Redis</li>
                                <li><strong>Cloud & DevOps:</strong> Cloud migration, Docker, CI/CD pipelines</li>
                                <li><strong>Frontend:</strong> Vue.js, WPF, JavaScript/TypeScript</li>
                                <li><strong>APIs:</strong> RESTful services, OAuth2/OIDC</li>
                                <li><strong>Mobile:</strong> Android (MAUI), native development</li>
                                <li><strong>Authentication:</strong> PKI, Certificate-based systems, Passwordless solutions</li>
                            </ul>
                        </div>
                    </div>

                    <div class="card shadow-sm mb-4">
                        <div class="card-body p-5">
                            <h2 class="h3 mb-4">
                                <i class="bi bi-rocket text-primary me-2"></i>
                                Personal Projects
                            </h2>
                            <p>
                                <strong>Passi</strong> - A passwordless OAuth2 2FA authentication solution demonstrating my passion 
                                for security and user experience:
                            </p>
                            <ul class="feature-list">
                                <li>Certificate-based authentication with public-key cryptography</li>
                                <li>Custom OpenID Connect (OIDC) server implementation from scratch</li>
                                <li>OAuth2 authorization server with PKCE-only authentication</li>
                                <li>Native Android application for mobile authentication</li>
                                <li>Full-stack implementation: ASP.NET Core, Vue.js, PostgreSQL, Redis</li>
                                <li>Open source projects available on GitHub</li>
                            </ul>
                            <p class="mb-0">
                                This project showcases my ability to bridge complex security requirements with practical, 
                                user-friendly solutions—eliminating passwords while enhancing security.
                            </p>
                        </div>
                    </div>

                    <div class="card shadow-sm mb-4 bg-light">
                        <div class="card-body p-5">
                            <h2 class="h3 mb-4">
                                <i class="bi bi-chat-quote text-primary me-2"></i>
                                Why I Build
                            </h2>
                            <blockquote class="blockquote mb-0">
                                <p class="fs-5">
                                    "The best technology is the technology you don't notice—until you realize 
                                    you can't live without it."
                                </p>
                            </blockquote>
                            <p class="mt-4 mb-0">
                                I create tools that disappear into the background, making people's digital lives easier 
                                while keeping them safe. That's what drives me every day.
                            </p>
                        </div>
                    </div>

                    <div class="text-center mt-5">
                        <h3 class="mb-4">Let's Connect</h3>
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
