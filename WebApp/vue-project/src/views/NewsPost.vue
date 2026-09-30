<script>
    import ReactionBar from '../components/ReactionBar.vue'
    import { getPost, formatDate } from '../news/newsApi'

    export default {
        components: { ReactionBar },
        data() {
            return {
                post: null,
                loading: true,
                notFound: false,
                error: '',
            }
        },
        watch: {
            '$route.params.slug': { handler: 'load', immediate: true },
        },
        methods: {
            formatDate,
            async load() {
                const slug = this.$route.params.slug
                if (!slug) return
                this.loading = true
                this.notFound = false
                this.error = ''
                try {
                    this.post = await getPost(slug)
                    document.title = `${this.post.title} · Passi News`
                } catch (e) {
                    if (e.status === 404) this.notFound = true
                    else this.error = 'Could not load this post.'
                } finally {
                    this.loading = false
                }
            },
        },
        unmounted() {
            document.title = 'Passi'
        },
    }
</script>

<template>
    <div class="container py-5 news-post">
        <router-link to="/news" class="back-link"><i class="bi bi-arrow-left me-1"></i>All news</router-link>

        <div v-if="loading" class="text-center py-5">
            <div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading</span></div>
        </div>
        <div v-else-if="notFound" class="py-5 text-center">
            <h1 class="h3">Post not found</h1>
            <p class="text-muted">It may have been moved or unpublished.</p>
        </div>
        <div v-else-if="error" class="alert alert-danger mt-4">{{ error }}</div>

        <article v-else-if="post" class="mt-4">
            <header class="mb-4">
                <small class="text-muted">{{ formatDate(post.publishedAt) }}</small>
                <h1 class="display-6 fw-bold mt-1">{{ post.title }}</h1>
                <p v-if="post.summary" class="lead text-muted">{{ post.summary }}</p>
            </header>

            <!-- Server-rendered Markdown: raw HTML is escaped and unsafe link schemes removed (NewsMarkdown). -->
            <div class="post-body" v-html="post.html"></div>

            <footer class="mt-5 pt-4 border-top">
                <p class="text-muted mb-2">What do you think?</p>
                <ReactionBar :slug="post.slug" :reactions="post.reactions" />
            </footer>
        </article>
    </div>
</template>

<style scoped>
.news-post {
    max-width: 760px;
}

.back-link {
    color: #667eea;
    text-decoration: none;
    font-weight: 600;
}

.post-body {
    font-size: 1.1rem;
    line-height: 1.75;
    overflow-wrap: anywhere;
}

.post-body :deep(img) {
    max-width: 100%;
    border-radius: 8px;
}

.post-body :deep(pre) {
    background: #f1f3f5;
    padding: 1rem;
    border-radius: 8px;
    overflow-x: auto;
}

.post-body :deep(code) {
    font-size: 0.9em;
}

.post-body :deep(table) {
    width: 100%;
    margin-bottom: 1rem;
    border-collapse: collapse;
}

.post-body :deep(th),
.post-body :deep(td) {
    border: 1px solid #dee2e6;
    padding: 0.5rem;
}

.post-body :deep(blockquote) {
    border-left: 4px solid #667eea;
    padding-left: 1rem;
    color: #6c757d;
}
</style>
