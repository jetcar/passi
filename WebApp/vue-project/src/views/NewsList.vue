<script>
    import ReactionBar from '../components/ReactionBar.vue'
    import { listPosts, loadContext, formatDate } from '../news/newsApi'

    const PAGE_SIZE = 10

    export default {
        components: { ReactionBar },
        data() {
            return {
                posts: [],
                loading: true,
                hasMore: false,
                error: '',
                isAdmin: false,
            }
        },
        async created() {
            loadContext().then(context => { this.isAdmin = context.isAdmin })
            await this.loadMore()
        },
        methods: {
            formatDate,
            async loadMore() {
                this.loading = true
                try {
                    const page = await listPosts(this.posts.length, PAGE_SIZE)
                    this.posts.push(...page)
                    this.hasMore = page.length === PAGE_SIZE
                } catch (e) {
                    this.error = 'Could not load news.'
                } finally {
                    this.loading = false
                }
            },
        },
    }
</script>

<template>
    <div class="news-page">
        <section class="news-hero text-white">
            <div class="container">
                <h1 class="display-5 fw-bold mb-2">News</h1>
                <p class="lead mb-0">Releases, changes and what's next for Passi.</p>
            </div>
        </section>

        <div class="container py-5">
            <div class="d-flex justify-content-end gap-2 mb-4">
                <router-link v-if="isAdmin" to="/news/admin" class="btn btn-outline-primary btn-sm">
                    <i class="bi bi-pencil-square me-1"></i>Manage posts
                </router-link>
                <a href="/news/rss.xml" class="btn btn-outline-secondary btn-sm" target="_blank">
                    <i class="bi bi-rss me-1"></i>RSS
                </a>
            </div>

            <div v-if="error" class="alert alert-danger">{{ error }}</div>
            <p v-else-if="!loading && posts.length === 0" class="text-muted text-center py-5">No news yet. Check back soon.</p>

            <article v-for="post in posts" :key="post.slug" class="news-card card mb-4">
                <div class="card-body p-4">
                    <small class="text-muted">{{ formatDate(post.publishedAt) }}</small>
                    <h2 class="h4 mt-1 mb-2">
                        <router-link :to="`/news/${post.slug}`" class="stretched-link-title">{{ post.title }}</router-link>
                    </h2>
                    <p v-if="post.summary" class="text-muted mb-3">{{ post.summary }}</p>
                    <div class="d-flex flex-wrap justify-content-between align-items-center gap-2">
                        <ReactionBar :slug="post.slug" :reactions="post.reactions" compact />
                        <router-link :to="`/news/${post.slug}`" class="read-more">Read more <i class="bi bi-arrow-right"></i></router-link>
                    </div>
                </div>
            </article>

            <div class="text-center">
                <div v-if="loading" class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading</span></div>
                <button v-else-if="hasMore" class="btn btn-outline-primary" @click="loadMore">Load more</button>
            </div>
        </div>
    </div>
</template>

<style scoped>
.news-hero {
    padding: 60px 0;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.news-card {
    border: none;
    border-radius: 15px;
    box-shadow: 0 5px 15px rgba(0, 0, 0, 0.08);
    transition: box-shadow 0.3s ease;
}

.news-card:hover {
    box-shadow: 0 10px 30px rgba(0, 0, 0, 0.12);
}

.stretched-link-title {
    color: #212529;
    text-decoration: none;
}

.stretched-link-title:hover {
    color: #667eea;
}

.read-more {
    color: #667eea;
    text-decoration: none;
    font-weight: 600;
}
</style>
