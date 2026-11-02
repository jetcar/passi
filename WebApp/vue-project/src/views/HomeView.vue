<template>
  <div class="home">
    <!-- Hero Section -->
    <section class="hero-section">
      <div class="container">
        <div class="row align-items-center min-vh-75">
          <div class="col-lg-6">
            <h1 class="display-3 fw-bold mb-4 animate-fade-in">
              {{ $t('home.title') }}
            </h1>
            <p class="lead mb-4 text-light">
              {{ $t('home.subtitle') }}
            </p>
            <div class="d-flex gap-3 flex-wrap">
              <router-link to="/products" class="btn btn-light btn-lg">
                <i class="bi bi-box-seam me-2"></i>{{ $t('home.exploreProducts') }}
              </router-link>
              <a href="https://www.youtube.com/watch?v=Koe_8YcmYkU" target="_blank" class="btn btn-outline-light btn-lg">
                <i class="bi bi-play-circle me-2"></i>{{ $t('common.watchDemo') }}
              </a>
            </div>
          </div>
          <div class="col-lg-6 text-center">
            <div class="hero-image">
              <i class="bi bi-fingerprint text-light" style="font-size: 15rem;"></i>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- Latest News Section -->
    <section v-if="news.length" class="news-section py-5">
      <div class="container">
        <div class="d-flex justify-content-between align-items-end mb-4">
          <div>
            <h2 class="display-6 fw-bold mb-1">{{ $t('home.latestNews') }}</h2>
            <p class="text-muted mb-0">{{ $t('home.latestSubtitle') }}</p>
          </div>
          <router-link to="/news" class="btn btn-outline-primary">{{ $t('home.allNews') }} <i class="bi bi-arrow-right ms-1"></i></router-link>
        </div>
        <div class="row g-4">
          <div v-for="post in news" :key="post.slug" class="col-md-4">
            <div class="news-card h-100 p-4 d-flex flex-column">
              <small class="text-muted">{{ formatDate(post.publishedAt) }}</small>
              <h3 class="h5 mt-1">
                <router-link :to="`/news/${post.slug}`" class="news-title">{{ post.title }}</router-link>
              </h3>
              <p class="text-muted flex-grow-1">{{ post.summary }}</p>
              <ReactionBar :slug="post.slug" :reactions="post.reactions" compact />
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- How It Works Section -->
    <section class="how-it-works-section py-5 bg-light">
      <div class="container">
        <div class="text-center mb-5">
          <h2 class="display-5 fw-bold">{{ $t('home.howTitle') }}</h2>
          <p class="text-muted">{{ $t('home.howSubtitle') }}</p>
        </div>
        <div class="row g-4">
          <div class="col-md-4">
            <div class="step-card text-center p-4">
              <div class="step-number mb-3">1</div>
              <div class="step-icon mb-3">
                <i class="bi bi-envelope-check-fill text-primary" style="font-size: 3rem;"></i>
              </div>
              <h4>{{ $t('home.confirmEmail') }}</h4>
              <p class="text-muted">{{ $t('home.confirmEmailText') }}</p>
            </div>
          </div>
          <div class="col-md-4">
            <div class="step-card text-center p-4">
              <div class="step-number mb-3">2</div>
              <div class="step-icon mb-3">
                <i class="bi bi-phone-fill text-success" style="font-size: 3rem;"></i>
              </div>
              <h4>{{ $t('home.mobileSetup') }}</h4>
              <p class="text-muted">{{ $t('home.mobileSetupText') }}</p>
            </div>
          </div>
          <div class="col-md-4">
            <div class="step-card text-center p-4">
              <div class="step-number mb-3">3</div>
              <div class="step-icon mb-3">
                <i class="bi bi-shield-check text-warning" style="font-size: 3rem;"></i>
              </div>
              <h4>{{ $t('home.secureLogin') }}</h4>
              <p class="text-muted">{{ $t('home.secureLoginText') }}</p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- Features Section -->
    <section class="features-section py-5">
      <div class="container">
        <div class="row align-items-center">
          <div class="col-lg-6">
            <h2 class="display-5 fw-bold mb-4">{{ $t('home.whyTitle') }}</h2>
            <div class="feature-list">
              <div class="feature-item mb-4">
                <div class="d-flex align-items-start">
                  <div class="feature-icon-box me-3">
                    <i class="bi bi-lock-fill"></i>
                  </div>
                  <div>
                    <h5>{{ $t('home.zeroTrust') }}</h5>
                    <p class="text-muted">{{ $t('home.zeroTrustText') }}</p>
                  </div>
                </div>
              </div>
              <div class="feature-item mb-4">
                <div class="d-flex align-items-start">
                  <div class="feature-icon-box me-3">
                    <i class="bi bi-github"></i>
                  </div>
                  <div>
                    <h5>{{ $t('home.openSource') }}</h5>
                    <i18n-t keypath="home.openSourceText" tag="p" class="text-muted">
                      <template #github><a href="https://github.com/jetcar/passi" target="_blank">GitHub</a></template>
                    </i18n-t>
                  </div>
                </div>
              </div>
              <div class="feature-item mb-4">
                <div class="d-flex align-items-start">
                  <div class="feature-icon-box me-3">
                    <i class="bi bi-lightning-fill"></i>
                  </div>
                  <div>
                    <h5>{{ $t('home.oauthStandard') }}</h5>
                    <p class="text-muted">{{ $t('home.oauthStandardText') }}</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
          <div class="col-lg-6">
            <div class="video-container">
              <a href="https://www.youtube.com/watch?v=Koe_8YcmYkU" target="_blank" class="video-thumbnail">
                <img src="/img/youtube.png" :alt="$t('common.watchDemo')" class="img-fluid rounded shadow-lg">
                <div class="play-overlay">
                  <i class="bi bi-play-circle-fill"></i>
                </div>
              </a>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- Download Section -->
    <section class="download-section py-5 bg-gradient text-white">
      <div class="container text-center">
        <h2 class="display-5 fw-bold mb-4">{{ $t('home.getStarted') }}</h2>
        <p class="lead mb-4">{{ $t('home.getStartedText') }}</p>
        <a href="https://play.google.com/store/apps/details?id=com.passi.cloud.passi_android" target="_blank">
          <img src="/img/en_badge_web_generic.png" :alt="$t('common.googlePlay')" style="max-width: 200px">
        </a>
      </div>
    </section>

    <!-- Open Source CTA -->
    <section class="cta-section py-5">
      <div class="container">
        <div class="row align-items-center">
          <div class="col-lg-8">
            <h3 class="fw-bold mb-2">{{ $t('home.ctaTitle') }}</h3>
            <p class="text-muted mb-0">{{ $t('home.ctaText') }}</p>
          </div>
          <div class="col-lg-4 text-lg-end mt-3 mt-lg-0">
            <a href="https://github.com/jetcar/passi" target="_blank" class="btn btn-dark btn-lg">
              <i class="bi bi-github me-2"></i>{{ $t('common.viewGithub') }}
            </a>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import ReactionBar from '../components/ReactionBar.vue'
// @ts-ignore - plain JS module
import { listPosts, formatDate } from '../news/newsApi'

const news = ref<any[]>([])

onMounted(() => {
  listPosts(0, 3).then((posts: any[]) => { news.value = posts }).catch(() => { news.value = [] })

  const observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        entry.target.classList.add('visible')
      }
    })
  })

  document.querySelectorAll('.step-card, .feature-item').forEach((el) => observer.observe(el))
})
</script>

<style scoped>
.hero-section {
  padding: 80px 0;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.min-vh-75 {
  min-height: 75vh;
}

.animate-fade-in {
  animation: fadeIn 1s ease-in;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.hero-image {
  animation: float 3s ease-in-out infinite;
}

@keyframes float {
  0%, 100% {
    transform: translateY(0);
  }
  50% {
    transform: translateY(-20px);
  }
}

.news-section {
  background: white;
}

.news-card {
  background: #f8f9fa;
  border-radius: 15px;
  transition: all 0.3s ease;
}

.news-card:hover {
  box-shadow: 0 10px 30px rgba(0,0,0,0.1);
  transform: translateY(-4px);
}

.news-title {
  color: #212529;
  text-decoration: none;
}

.news-title:hover {
  color: #667eea;
}

.how-it-works-section {
  padding: 80px 0;
}

.step-card {
  background: white;
  border-radius: 15px;
  box-shadow: 0 5px 15px rgba(0,0,0,0.08);
  transition: all 0.6s ease;
  opacity: 0;
  transform: translateY(30px);
  position: relative;
  overflow: hidden;
}

.step-card.visible {
  opacity: 1;
  transform: translateY(0);
}

.step-card:hover {
  transform: translateY(-10px);
  box-shadow: 0 10px 30px rgba(0,0,0,0.15);
}

.step-number {
  position: absolute;
  top: 10px;
  right: 15px;
  width: 40px;
  height: 40px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  font-size: 1.2rem;
}

.features-section {
  padding: 80px 0;
}

.feature-icon-box {
  width: 50px;
  height: 50px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-size: 1.5rem;
  flex-shrink: 0;
}

.feature-item {
  opacity: 0;
  transform: translateX(-30px);
  transition: all 0.6s ease;
}

.feature-item.visible {
  opacity: 1;
  transform: translateX(0);
}

.video-container {
  position: relative;
}

.video-thumbnail {
  position: relative;
  display: block;
  transition: transform 0.3s ease;
}

.video-thumbnail:hover {
  transform: scale(1.05);
}

.play-overlay {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  font-size: 5rem;
  color: white;
  text-shadow: 0 5px 15px rgba(0,0,0,0.5);
  pointer-events: none;
}

.download-section {
  padding: 60px 0;
}

.bg-gradient {
  background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%);
}

.cta-section {
  padding: 60px 0;
  background: #f8f9fa;
}

@media (max-width: 768px) {
  .hero-section {
    padding: 60px 0;
  }
  
  .step-card {
    margin-bottom: 20px;
  }
}
</style>
