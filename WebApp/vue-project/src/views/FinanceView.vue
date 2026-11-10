<template>
  <div class="finance-page" lang="en">
    <section class="finance-hero text-white py-5">
      <div class="container py-lg-4">
        <div class="row align-items-center g-5">
          <div class="col-lg-7">
            <span class="eyebrow">PERSONAL FINANCE</span>
            <h1 class="display-4 fw-bold mt-3 mb-3">Understand where your money goes</h1>
            <p class="lead mb-4">
              Passi Finance is a private expense analyzer for bringing bank transactions,
              spending categories, and monthly budgets into one clear view.
            </p>
            <div class="d-flex flex-wrap gap-3">
              <a
                href="https://finance.passi.cloud/"
                target="_blank"
                rel="noopener noreferrer"
                class="btn btn-light btn-lg"
              >
                <i class="bi bi-box-arrow-up-right me-2"></i>Open Passi Finance
              </a>
              <router-link to="/products" class="btn btn-outline-light btn-lg">
                <i class="bi bi-arrow-left me-2"></i>All Products
              </router-link>
            </div>
          </div>
          <div class="col-lg-5">
            <div class="summary-card">
              <i class="bi bi-pie-chart-fill"></i>
              <h2 class="h4 mt-3">Your month at a glance</h2>
              <p class="mb-0">
                Review accounts, transactions, category totals, and remaining budgets without
                building a spreadsheet by hand.
              </p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <section class="py-5">
      <div class="container">
        <div class="text-center mb-5">
          <h2 class="fw-bold">What you can do</h2>
          <p class="text-muted mb-0">From bank synchronization to a useful monthly spending picture.</p>
        </div>
        <div class="row g-4">
          <div class="col-md-6 col-lg-4" v-for="feature in features" :key="feature.title">
            <article class="feature-card h-100">
              <div class="feature-icon"><i :class="feature.icon"></i></div>
              <h3 class="h5">{{ feature.title }}</h3>
              <p class="text-muted mb-0">{{ feature.description }}</p>
            </article>
          </div>
        </div>
      </div>
    </section>

    <section class="privacy-section py-5">
      <div class="container">
        <div class="row align-items-center g-4">
          <div class="col-lg-8">
            <span class="eyebrow text-success">PRIVACY BY DESIGN</span>
            <h2 class="fw-bold mt-2">Built for personal use</h2>
            <p class="text-muted mb-0">
              Bank data is accessed only with your consent. Passi provides sign-in, and Enable Banking
              connects to your bank. The service does not sell personal data, use it for advertising,
              or initiate payments. Read the privacy notice for storage details and your choices.
            </p>
          </div>
          <div class="col-lg-4 text-lg-end">
            <a
              href="https://finance.passi.cloud/privacy"
              target="_blank"
              rel="noopener noreferrer"
              class="btn btn-outline-success"
            >
              <i class="bi bi-shield-check me-2"></i>Read Privacy Notice
            </a>
          </div>
        </div>
      </div>
    </section>

    <section class="py-5">
      <div class="container text-center">
        <h2 class="fw-bold">Ready to review your spending?</h2>
        <p class="text-muted mb-4">Try the demo, or sign in with Passi to access your personal finances. Personal account access is restricted.</p>
        <a
          href="https://finance.passi.cloud/"
          target="_blank"
          rel="noopener noreferrer"
          class="btn btn-success btn-lg"
        >
          Go to Passi Finance <i class="bi bi-arrow-right ms-2"></i>
        </a>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'

onMounted(() => window.scrollTo(0, 0))

const features = [
  {
    icon: 'bi bi-bank',
    title: 'Connect Swedbank',
    description: 'Connect through Enable Banking, choose which accounts to track, and synchronize transactions.'
  },
  {
    icon: 'bi bi-calendar3',
    title: 'Monthly overview',
    description: 'See transaction counts, total spending, and account activity for the selected month.'
  },
  {
    icon: 'bi bi-tags-fill',
    title: 'Organize expenses',
    description: 'Create categories, filter transactions, and move expenses where they belong.'
  },
  {
    icon: 'bi bi-speedometer2',
    title: 'Track budgets',
    description: 'Set monthly category budgets and see what is spent, remaining, or over budget.'
  },
  {
    icon: 'bi bi-diagram-3-fill',
    title: 'Split transactions',
    description: 'Divide one expense across multiple categories when a single purchase covers several needs.'
  },
  {
    icon: 'bi bi-translate',
    title: 'Use your language',
    description: 'Use the application in English, Estonian, or Russian.'
  }
]
</script>

<style scoped>
.finance-hero {
  background: linear-gradient(135deg, #103c32 0%, #198754 55%, #20c997 100%);
}

.eyebrow {
  font-size: 0.8rem;
  font-weight: 700;
  letter-spacing: 0.14em;
}

.summary-card {
  padding: 2rem;
  border: 1px solid rgba(255, 255, 255, 0.35);
  border-radius: 1.25rem;
  background: rgba(255, 255, 255, 0.12);
  box-shadow: 0 1rem 2.5rem rgba(3, 41, 30, 0.2);
  backdrop-filter: blur(8px);
}

.summary-card > i {
  font-size: 3rem;
}

.feature-card {
  padding: 1.75rem;
  border: 1px solid #e2e8f0;
  border-radius: 1rem;
  background: white;
  box-shadow: 0 0.5rem 1.5rem rgba(24, 88, 68, 0.07);
}

.feature-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 3rem;
  height: 3rem;
  margin-bottom: 1rem;
  border-radius: 0.8rem;
  color: white;
  background: linear-gradient(135deg, #20c997, #198754);
  font-size: 1.3rem;
}

.privacy-section {
  background: #eefaf5;
  border-top: 1px solid #d7f1e6;
  border-bottom: 1px solid #d7f1e6;
}
</style>
