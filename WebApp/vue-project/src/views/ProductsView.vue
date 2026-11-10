<script setup lang="ts">
import { onMounted } from 'vue'

type Action = { href: string; labelKey: string; icon: string; className: string; route?: boolean }
type Product = {
  key: string
  icon: string
  iconClass: string
  badgeKey: string
  badgeClass: string
  featureClass: string
  actions: Action[]
  extra?: 'demo' | 'star'
  playStore?: boolean
}

const products: Product[] = [
  {
    key: 'finance', icon: 'bi-wallet2', iconClass: 'bg-finance', badgeKey: 'personalFinance', badgeClass: 'bg-success', featureClass: 'text-success',
    actions: [
      { href: '/products/finance', labelKey: 'products.items.finance.learnMore', icon: 'bi-info-circle', className: 'btn-success', route: true },
      { href: 'https://finance.passi.cloud/', labelKey: 'products.items.finance.open', icon: 'bi-box-arrow-up-right', className: 'btn-outline-secondary' },
    ],
  },
  {
    key: 'passi', icon: 'bi-fingerprint', iconClass: '', badgeKey: 'openSource', badgeClass: 'bg-success', featureClass: 'text-success',
    actions: [
      { href: 'https://github.com/jetcar/passi', labelKey: 'common.viewGithub', icon: 'bi-github', className: 'btn-primary' },
      { href: '/Home/RegisterApp', labelKey: 'products.registerWebsite', icon: 'bi-tools', className: 'btn-outline-secondary' },
    ], extra: 'demo', playStore: true,
  },
  {
    key: 'eid', icon: 'bi-credit-card-2-front-fill', iconClass: 'bg-info', badgeKey: 'openSource', badgeClass: 'bg-info', featureClass: 'text-info',
    actions: [
      { href: 'https://github.com/jetcar/eid-openidc', labelKey: 'common.viewGithub', icon: 'bi-github', className: 'btn-info text-white' },
      { href: 'https://github.com/jetcar/eid-openidc#readme', labelKey: 'common.documentation', icon: 'bi-book', className: 'btn-outline-secondary' },
    ], extra: 'star',
  },
  {
    key: 'crashes', icon: 'bi-car-front-fill', iconClass: 'bg-danger', badgeKey: 'openData', badgeClass: 'bg-danger', featureClass: 'text-danger',
    actions: [
      { href: 'https://github.com/jetcar/tallinn-crashes', labelKey: 'common.viewGithub', icon: 'bi-github', className: 'btn-danger' },
      { href: 'https://github.com/jetcar/tallinn-crashes/blob/main/REPORT.md', labelKey: 'common.report', icon: 'bi-bar-chart', className: 'btn-outline-secondary' },
    ],
  },
  {
    key: 'streamdek', icon: 'bi-grid-3x3-gap-fill', iconClass: 'bg-streamdek', badgeKey: 'openSource', badgeClass: 'bg-primary', featureClass: 'text-primary',
    actions: [
      { href: 'https://github.com/jetcar/streamdek', labelKey: 'common.viewGithub', icon: 'bi-github', className: 'btn-primary' },
      { href: 'https://github.com/jetcar/streamdek/releases/latest', labelKey: 'common.download', icon: 'bi-download', className: 'btn-outline-secondary' },
    ],
  },
  {
    key: 'economy', icon: 'bi-diagram-3-fill', iconClass: 'bg-success', badgeKey: 'openResearch', badgeClass: 'bg-success', featureClass: 'text-success',
    actions: [
      { href: 'https://github.com/jetcar/deksirip', labelKey: 'common.viewGithub', icon: 'bi-github', className: 'btn-success' },
      { href: 'https://github.com/jetcar/deksirip/blob/main/README.en.md', labelKey: 'products.readConcept', icon: 'bi-book', className: 'btn-outline-secondary' },
    ],
  },
  {
    key: 'charp2p', icon: 'bi-chat-dots-fill', iconClass: 'bg-warning', badgeKey: 'inDevelopment', badgeClass: 'bg-warning text-dark', featureClass: 'text-warning',
    actions: [
      { href: 'https://github.com/jetcar/charp2p', labelKey: 'common.viewGithub', icon: 'bi-github', className: 'btn-warning' },
      { href: 'https://github.com/jetcar/charp2p#readme', labelKey: 'common.documentation', icon: 'bi-book', className: 'btn-outline-secondary' },
    ],
  },
]

const featureKeys = (product: Product) => Array.from({ length: 5 }, (_, index) => `products.items.${product.key}.feature${index + 1}`)

onMounted(() => {
  const observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) entry.target.classList.add('visible')
    })
  })
  document.querySelectorAll('.product-card').forEach(el => observer.observe(el))
})
</script>

<template>
  <div class="products-page">
    <section class="products-header bg-gradient text-white py-5">
      <div class="container text-center">
        <h1 class="display-4 fw-bold mb-3">{{ $t('products.title') }}</h1>
        <p class="lead">{{ $t('products.subtitle') }}</p>
      </div>
    </section>

    <section class="products-section py-5">
      <div class="container">
        <div class="row g-4">
          <div v-for="product in products" :key="product.key" class="col-lg-6">
            <div class="product-card h-100">
              <div class="product-header">
                <div class="product-icon" :class="product.iconClass"><i class="bi" :class="product.icon"></i></div>
                <h3 class="product-title">{{ $t(`products.items.${product.key}.title`) }}</h3>
                <span class="badge" :class="product.badgeClass">{{ $t(`products.badges.${product.badgeKey}`) }}</span>
              </div>
              <div class="product-body">
                <p class="product-description">{{ $t(`products.items.${product.key}.description`) }}</p>
                <div class="product-features mb-4">
                  <h5>{{ $t('products.keyFeatures') }}</h5>
                  <ul>
                    <li v-for="key in featureKeys(product)" :key="key">
                      <i class="bi bi-check-circle-fill" :class="product.featureClass"></i>{{ $t(key) }}
                    </li>
                  </ul>
                </div>
                <div class="product-actions">
                  <template v-for="action in product.actions" :key="action.href">
                    <router-link v-if="action.route" :to="action.href" class="btn" :class="action.className">
                      <i class="bi me-2" :class="action.icon"></i>{{ $t(action.labelKey) }}
                    </router-link>
                    <a v-else :href="action.href" target="_blank" rel="noopener noreferrer" class="btn" :class="action.className">
                      <i class="bi me-2" :class="action.icon"></i>{{ $t(action.labelKey) }}
                    </a>
                  </template>
                </div>
                <div class="product-tech-stack mt-4">
                  <small class="text-muted">{{ $t(`products.items.${product.key}.tech`) }}</small>
                </div>
                <div v-if="product.extra === 'demo'" class="mt-3">
                  <a href="https://www.youtube.com/watch?v=Koe_8YcmYkU" target="_blank" class="text-decoration-none">
                    <i class="bi bi-play-circle-fill text-danger"></i><span class="text-muted ms-1">{{ $t('products.watchDemoVideo') }}</span>
                  </a>
                </div>
                <div v-if="product.extra === 'star'" class="mt-3">
                  <a href="https://github.com/jetcar/eid-openidc" target="_blank" class="text-decoration-none">
                    <i class="bi bi-star-fill text-warning"></i><span class="text-muted ms-1">{{ $t('products.starGithub') }}</span>
                  </a>
                </div>
                <div v-if="product.playStore" class="mt-3">
                  <a href="https://play.google.com/store/apps/details?id=com.passi.cloud.passi_android" target="_blank">
                    <img src="/img/en_badge_web_generic.png" :alt="$t('common.googlePlay')" style="max-width: 150px">
                  </a>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="row mt-5">
          <div class="col-12">
            <div class="info-box p-4 bg-light rounded">
              <h4 class="mb-3"><i class="bi bi-info-circle-fill text-primary me-2"></i>{{ $t('products.infoTitle') }}</h4>
              <p class="mb-0">{{ $t('products.infoText') }}</p>
            </div>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<style scoped>
.products-header { padding: 60px 0; }
.bg-gradient { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); }
.products-section { padding: 60px 0; }
.product-card { background: white; border-radius: 20px; box-shadow: 0 10px 30px rgba(0,0,0,.1); overflow: hidden; transition: all .4s ease; opacity: 0; transform: translateY(30px); }
.product-card.visible { opacity: 1; transform: translateY(0); }
.product-card:hover { transform: translateY(-10px); box-shadow: 0 20px 40px rgba(0,0,0,.15); }
.product-header { padding: 2rem; background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%); position: relative; }
.product-icon { width: 80px; height: 80px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 20px; display: flex; align-items: center; justify-content: center; margin-bottom: 1rem; box-shadow: 0 5px 15px rgba(102,126,234,.3); }
.product-icon.bg-info { background: linear-gradient(135deg, #0dcaf0 0%, #0a7ea3 100%); }
.product-icon.bg-finance { background: linear-gradient(135deg, #20c997 0%, #198754 100%); }
.product-icon.bg-streamdek { background: linear-gradient(135deg, #2563eb 0%, #7c3aed 100%); }
.product-icon i { font-size: 2.5rem; color: white; }
.product-title { font-size: 1.75rem; font-weight: bold; margin-bottom: .5rem; color: #2d3748; }
.product-body { padding: 2rem; }
.product-description { color: #4a5568; font-size: 1.05rem; line-height: 1.7; margin-bottom: 1.5rem; }
.product-features ul { list-style: none; padding: 0; }
.product-features li { padding: .5rem 0; color: #4a5568; }
.product-features i { margin-right: .5rem; }
.product-actions { display: flex; gap: 1rem; flex-wrap: wrap; }
.product-actions .btn { flex: 1; min-width: 150px; }
.product-tech-stack { padding-top: 1rem; border-top: 1px solid #e2e8f0; }
.info-box { border-left: 4px solid #667eea; }
@media (max-width: 768px) { .product-actions { flex-direction: column; } .product-actions .btn { width: 100%; } }
</style>
