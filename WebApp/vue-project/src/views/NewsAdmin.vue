<script>
    import { admin, loadContext, formatDate } from '../news/newsApi'

    const emptyForm = () => ({ title: '', slug: '', summary: '', bodyMarkdown: '' })

    export default {
        data() {
            return {
                allowed: null,
                posts: [],
                loading: true,
                error: '',
                message: '',
                // null = list view, '' = new post, otherwise the slug being edited
                editingSlug: null,
                form: emptyForm(),
                previewHtml: '',
                showPreview: false,
                saving: false,
                mcpUrl: `${window.location.origin}/mcp`,
            }
        },
        async created() {
            const context = await loadContext()
            this.allowed = context.isAdmin
            if (this.allowed) await this.refresh()
            else this.loading = false
        },
        methods: {
            formatDate,
            async run(action, message) {
                this.error = ''
                this.message = ''
                try {
                    const result = await action()
                    if (message) this.message = message
                    return result
                } catch (e) {
                    this.error = e.message
                    return undefined
                }
            },
            async refresh() {
                this.loading = true
                this.posts = (await this.run(() => admin.list())) || []
                this.loading = false
            },
            newPost() {
                this.editingSlug = ''
                this.form = emptyForm()
                this.showPreview = false
            },
            edit(post) {
                this.editingSlug = post.slug
                this.form = { title: post.title, slug: post.slug, summary: post.summary, bodyMarkdown: post.bodyMarkdown }
                this.showPreview = false
            },
            cancel() {
                this.editingSlug = null
            },
            async togglePreview() {
                this.showPreview = !this.showPreview
                if (this.showPreview) {
                    const result = await this.run(() => admin.preview(this.form.bodyMarkdown))
                    this.previewHtml = result ? result.html : ''
                }
            },
            async save(publish) {
                this.saving = true
                const isNew = this.editingSlug === ''
                let post = await this.run(() => isNew ? admin.create(this.form) : admin.update(this.editingSlug, this.form))
                if (post && publish && !post.publishedAt) post = await this.run(() => admin.publish(post.slug))
                this.saving = false
                if (post) {
                    this.message = publish ? this.$t('news.admin.publishedMessage') : this.$t('news.admin.savedMessage')
                    this.editingSlug = null
                    await this.refresh()
                }
            },
            async setPublished(post, published) {
                await this.run(() => published ? admin.publish(post.slug) : admin.unpublish(post.slug), published ? this.$t('news.admin.publishedMessage') : this.$t('news.admin.draftMessage'))
                await this.refresh()
            },
            async remove(post) {
                if (!window.confirm(this.$t('news.admin.confirmDelete', { title: post.title }))) return
                await this.run(() => admin.remove(post.slug), this.$t('news.admin.deletedMessage'))
                await this.refresh()
            },
        },
    }
</script>

<template>
    <div class="container py-5">
        <div class="d-flex justify-content-between align-items-center mb-4">
            <h1 class="h2 mb-0">{{ $t('news.admin.title') }}</h1>
            <router-link to="/news" class="btn btn-link">{{ $t('news.admin.viewNews') }}</router-link>
        </div>

        <div v-if="allowed === false" class="alert alert-warning">
            {{ $t('news.admin.adminOnly') }} <a href="/Auth/Login">{{ $t('navigation.login') }}</a> {{ $t('news.admin.withAdmin') }}
        </div>

        <template v-else-if="allowed">
            <div v-if="error" class="alert alert-danger">{{ error }}</div>
            <div v-if="message" class="alert alert-success">{{ message }}</div>

            <!-- Editor -->
            <div v-if="editingSlug !== null" class="card mb-4">
                <div class="card-body">
                    <h2 class="h5 mb-3">{{ editingSlug === '' ? $t('news.admin.newPost') : $t('news.admin.editPost') }}</h2>
                    <div class="mb-3">
                        <label class="form-label" for="news-title">{{ $t('news.admin.titleLabel') }}</label>
                        <input id="news-title" v-model="form.title" class="form-control" maxlength="200" required>
                    </div>
                    <div class="mb-3">
                        <label class="form-label" for="news-slug">{{ $t('news.admin.slugLabel') }} <small class="text-muted">({{ $t('news.admin.slugHint') }})</small></label>
                        <input id="news-slug" v-model="form.slug" class="form-control" maxlength="80" :placeholder="$t('news.admin.slugPlaceholder')">
                    </div>
                    <div class="mb-3">
                        <label class="form-label" for="news-summary">{{ $t('news.admin.summaryLabel') }} <small class="text-muted">({{ $t('news.admin.summaryHint') }})</small></label>
                        <textarea id="news-summary" v-model="form.summary" class="form-control" rows="2" maxlength="500"></textarea>
                    </div>
                    <div class="mb-3">
                        <div class="d-flex justify-content-between align-items-end">
                            <label class="form-label" for="news-body">{{ $t('news.admin.bodyLabel') }} <small class="text-muted">(Markdown)</small></label>
                            <button type="button" class="btn btn-sm btn-outline-secondary mb-2" @click="togglePreview">
                                {{ showPreview ? $t('common.edit') : $t('common.preview') }}
                            </button>
                        </div>
                        <textarea v-if="!showPreview" id="news-body" v-model="form.bodyMarkdown" class="form-control font-monospace" rows="14"></textarea>
                        <div v-else class="preview border rounded p-3" v-html="previewHtml"></div>
                    </div>
                    <div class="d-flex flex-wrap gap-2">
                        <button class="btn btn-primary" :disabled="saving || !form.title" @click="save(true)">{{ $t('news.admin.savePublish') }}</button>
                        <button class="btn btn-outline-primary" :disabled="saving || !form.title" @click="save(false)">{{ $t('common.save') }}</button>
                        <button class="btn btn-link" :disabled="saving" @click="cancel">{{ $t('common.cancel') }}</button>
                    </div>
                </div>
            </div>

            <!-- List -->
            <template v-else>
                <button class="btn btn-primary mb-3" @click="newPost"><i class="bi bi-plus-lg me-1"></i>{{ $t('news.admin.newPost') }}</button>
                <div v-if="loading" class="spinner-border text-primary" role="status"><span class="visually-hidden">{{ $t('common.loading') }}</span></div>
                <p v-else-if="posts.length === 0" class="text-muted">{{ $t('news.admin.noPosts') }}</p>
                <div v-else class="list-group mb-4">
                    <div v-for="post in posts" :key="post.slug" class="list-group-item d-flex flex-wrap justify-content-between align-items-center gap-2">
                        <div>
                            <span class="badge me-2" :class="post.publishedAt ? 'bg-success' : 'bg-secondary'">
                                {{ post.publishedAt ? $t('news.admin.published') : $t('news.admin.draft') }}
                            </span>
                            <strong>{{ post.title }}</strong>
                            <small class="text-muted ms-2">{{ post.publishedAt ? formatDate(post.publishedAt) : $t('news.admin.updated', { date: formatDate(post.updatedAt) }) }}</small>
                        </div>
                        <div class="btn-group btn-group-sm">
                            <router-link v-if="post.publishedAt" :to="`/news/${post.slug}`" class="btn btn-outline-secondary">{{ $t('common.view') }}</router-link>
                            <button class="btn btn-outline-primary" @click="edit(post)">{{ $t('common.edit') }}</button>
                            <button v-if="post.publishedAt" class="btn btn-outline-warning" @click="setPublished(post, false)">{{ $t('news.admin.unpublish') }}</button>
                            <button v-else class="btn btn-outline-success" @click="setPublished(post, true)">{{ $t('news.admin.publish') }}</button>
                            <button class="btn btn-outline-danger" @click="remove(post)">{{ $t('common.delete') }}</button>
                        </div>
                    </div>
                </div>

                <div class="card bg-light border-0">
                    <div class="card-body">
                        <h2 class="h6"><i class="bi bi-robot me-1"></i>{{ $t('news.admin.mcpTitle') }}</h2>
                        <p class="small mb-2">
                            {{ $t('news.admin.mcpAgents') }} <code>{{ mcpUrl }}</code>.
                            {{ $t('news.admin.mcpRegister') }} <router-link to="/OAuthApps">{{ $t('navigation.oauthApps') }}</router-link>
                            {{ $t('news.admin.mcpRedirect') }} <code>http://localhost:8765/callback</code>.
                            {{ $t('news.admin.mcpClientId') }} <code>NewsMcpClientIds</code>. {{ $t('news.admin.mcpThen') }}
                        </p>
                        <pre class="small mb-0"><code>claude mcp add --transport http passi-news {{ mcpUrl }} --client-id &lt;client id&gt; --callback-port 8765</code></pre>
                    </div>
                </div>
            </template>
        </template>
    </div>
</template>

<style scoped>
.preview {
    min-height: 200px;
    background: white;
    overflow-wrap: anywhere;
}

.preview :deep(img) {
    max-width: 100%;
}

pre {
    white-space: pre-wrap;
    word-break: break-all;
}
</style>
