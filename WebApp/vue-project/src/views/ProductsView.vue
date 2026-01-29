<template>
  <div class="products-page">
    <!-- Header -->
    <section class="products-header bg-gradient text-white py-5">
      <div class="container">
        <div class="text-center">
          <h1 class="display-4 fw-bold mb-3">Our Products</h1>
          <p class="lead">Innovative authentication and identity solutions</p>
        </div>
      </div>
    </section>

    <!-- Products Grid -->
    <section class="products-section py-5">
      <div class="container">
        <div class="row g-4">
          
          <!-- Passi Passwordless Auth -->
          <div class="col-lg-6">
            <div class="product-card h-100">
              <div class="product-header">
                <div class="product-icon">
                  <i class="bi bi-fingerprint"></i>
                </div>
                <h3 class="product-title">Passi Passwordless Auth</h3>
                <span class="badge bg-success">Open Source</span>
              </div>
              <div class="product-body">
                <p class="product-description">
                  Revolutionary passwordless authentication system powered by OAuth2 and certificate-based security. 
                  No passwords to remember, no passwords to steal - just seamless, secure authentication.
                </p>
                
                <div class="product-features mb-4">
                  <h5>Key Features:</h5>
                  <ul>
                    <li><i class="bi bi-check-circle-fill text-success"></i> OAuth2 Protocol</li>
                    <li><i class="bi bi-check-circle-fill text-success"></i> Certificate-Based Auth</li>
                    <li><i class="bi bi-check-circle-fill text-success"></i> Zero-Trust Security</li>
                    <li><i class="bi bi-check-circle-fill text-success"></i> Mobile App Integration</li>
                    <li><i class="bi bi-check-circle-fill text-success"></i> Email Verification</li>
                  </ul>
                </div>

                <div class="product-actions">
                  <a href="https://github.com/jetcar/passi" target="_blank" class="btn btn-primary">
                    <i class="bi bi-github me-2"></i>View on GitHub
                  </a>
                  <a href="/Home/DevTools" class="btn btn-outline-secondary">
                    <i class="bi bi-tools me-2"></i>Register Website
                  </a>
                </div>

                <div class="product-tech-stack mt-4">
                  <small class="text-muted">Built with: ASP.NET Core, Vue.js, OAuth2</small>
                </div>

                <div class="mt-3">
                  <a href="https://www.youtube.com/watch?v=tRrWp6LWQNU" target="_blank" class="text-decoration-none">
                    <i class="bi bi-play-circle-fill text-danger"></i>
                    <span class="text-muted ms-1">Watch Demo Video</span>
                  </a>
                </div>

                <div class="mt-3">
                  <a href="https://play.google.com/store/apps/details?id=com.passi.cloud.passi_android" target="_blank">
                    <img src="/img/en_badge_web_generic.png" alt="Get it on Google Play" style="max-width: 150px">
                  </a>
                </div>
              </div>
            </div>
          </div>

          <!-- eID OpenID Connect -->
          <div class="col-lg-6">
            <div class="product-card h-100">
              <div class="product-header">
                <div class="product-icon bg-info">
                  <i class="bi bi-credit-card-2-front-fill"></i>
                </div>
                <h3 class="product-title">eID OpenID Connect</h3>
                <span class="badge bg-info">Open Source</span>
              </div>
              <div class="product-body">
                <p class="product-description">
                  An innovative OpenID Connect implementation based on SK solutions, supporting 
                  Smart-ID, Mobile-ID, and ID-card authentication services. Provides secure, 
                  government-grade authentication for your applications with full OpenIDC support.
                </p>
                
                <div class="product-features mb-4">
                  <h5>Key Features:</h5>
                  <ul>
                    <li><i class="bi bi-check-circle-fill text-info"></i> eID Card Authentication</li>
                    <li><i class="bi bi-check-circle-fill text-info"></i> OpenID Connect Protocol</li>
                    <li><i class="bi bi-check-circle-fill text-info"></i> Government-grade Security</li>
                    <li><i class="bi bi-check-circle-fill text-info"></i> Multi-platform Support</li>
                    <li><i class="bi bi-check-circle-fill text-info"></i> Easy Integration</li>
                  </ul>
                </div>

                <div class="product-actions">
                  <a href="https://github.com/jetcar/eid-openidc" target="_blank" class="btn btn-info text-white">
                    <i class="bi bi-github me-2"></i>View on GitHub
                  </a>
                  <a href="https://github.com/jetcar/eid-openidc#readme" target="_blank" class="btn btn-outline-secondary">
                    <i class="bi bi-book me-2"></i>Documentation
                  </a>
                </div>

                <div class="product-tech-stack mt-4">
                  <small class="text-muted">Built with: Java, OpenID Connect, SK Solutions APIs</small>
                </div>

                <div class="github-stats mt-3">
                  <a href="https://github.com/jetcar/eid-openidc" target="_blank" class="text-decoration-none">
                    <i class="bi bi-star-fill text-warning"></i>
                    <span class="text-muted ms-1">Star on GitHub</span>
                  </a>
                </div>
              </div>
            </div>
          </div>

        </div>

        <!-- Additional Info Section -->
        <div class="row mt-5">
          <div class="col-12">
            <div class="info-box p-4 bg-light rounded">
              <h4 class="mb-3">
                <i class="bi bi-info-circle-fill text-primary me-2"></i>
                Open Source & Transparent
              </h4>
              <p class="mb-0">
                Both products are completely open source and available on GitHub. 
                We believe in transparency and invite you to review our code, contribute to development, 
                and build secure authentication solutions for your own projects.
              </p>
            </div>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'

onMounted(() => {
  const observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        entry.target.classList.add('visible')
      }
    })
  })

  document.querySelectorAll('.product-card').forEach((el) => observer.observe(el))
})
</script>

<style scoped>
.products-header {
  padding: 60px 0;
}

.bg-gradient {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.products-section {
  padding: 60px 0;
}

.product-card {
  background: white;
  border-radius: 20px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  transition: all 0.4s ease;
  opacity: 0;
  transform: translateY(30px);
}

.product-card.visible {
  opacity: 1;
  transform: translateY(0);
}

.product-card:hover {
  transform: translateY(-10px);
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.15);
}

.product-header {
  padding: 2rem;
  background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
  position: relative;
}

.product-icon {
  width: 80px;
  height: 80px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 1rem;
  box-shadow: 0 5px 15px rgba(102, 126, 234, 0.3);
}

.product-icon.bg-info {
  background: linear-gradient(135deg, #0dcaf0 0%, #0a7ea3 100%);
}

.product-icon i {
  font-size: 2.5rem;
  color: white;
}

.product-title {
  font-size: 1.75rem;
  font-weight: bold;
  margin-bottom: 0.5rem;
  color: #2d3748;
}

.product-body {
  padding: 2rem;
}

.product-description {
  color: #4a5568;
  font-size: 1.05rem;
  line-height: 1.7;
  margin-bottom: 1.5rem;
}

.product-features ul {
  list-style: none;
  padding: 0;
}

.product-features li {
  padding: 0.5rem 0;
  color: #4a5568;
}

.product-features i {
  margin-right: 0.5rem;
}

.product-actions {
  display: flex;
  gap: 1rem;
  flex-wrap: wrap;
}

.product-actions .btn {
  flex: 1;
  min-width: 150px;
}

.product-tech-stack {
  padding-top: 1rem;
  border-top: 1px solid #e2e8f0;
}

.info-box {
  border-left: 4px solid #667eea;
}

@media (max-width: 768px) {
  .product-actions {
    flex-direction: column;
  }
  
  .product-actions .btn {
    width: 100%;
  }
}
</style>
