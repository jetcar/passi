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
                    this.message = publish ? 'Published.' : 'Saved.'
                    this.editingSlug = null
                    await this.refresh()
                }
            },
            async setPublished(post, published) {
                await this.run(() => published ? admin.publish(post.slug) : admin.unpublish(post.slug), published ? 'Published.' : 'Moved back to drafts.')
                await this.refresh()
            },
            async remove(post) {
                if (!window.confirm(`Delete "${post.title}" and its reactions? This cannot be undone.`)) return
                await this.run(() => admin.remove(post.slug), 'Deleted.')
                await this.refresh()
            },
        },
    }
</script>

<template>
    <div class="container py-5">
        <div class="d-flex justify-content-between align-items-center mb-4">
            <h1 class="h2 mb-0">Manage news</h1>
            <router-link to="/news" class="btn btn-link">View news</router-link>
        </div>

        <div v-if="allowed === false" class="alert alert-warning">
            Only news admins can manage posts. <a href="/Auth/Login">Log in</a> with an admin account.
        </div>

        <template v-else-if="allowed">
            <div v-if="error" class="alert alert-danger">{{ error }}</div>
            <div v-if="message" class="alert alert-success">{{ message }}</div>

            <!-- Editor -->
            <div v-if="editingSlug !== null" class="card mb-4">
                <div class="card-body">
                    <h2 class="h5 mb-3">{{ editingSlug === '' ? 'New post' : 'Edit post' }}</h2>
                    <div class="mb-3">
                        <label class="form-label" for="news-title">Title</label>
                        <input id="news-title" v-model="form.title" class="form-control" maxlength="200" required>
                    </div>
                    <div class="mb-3">
                        <label class="form-label" for="news-slug">Slug <small class="text-muted">(URL; generated from the title when empty)</small></label>
                        <input id="news-slug" v-model="form.slug" class="form-control" maxlength="80" placeholder="e.g. faster-logins">
                    </div>
                    <div class="mb-3">
                        <label class="form-label" for="news-summary">Summary <small class="text-muted">(shown on the home page and list)</small></label>
                        <textarea id="news-summary" v-model="form.summary" class="form-control" rows="2" maxlength="500"></textarea>
                    </div>
                    <div class="mb-3">
                        <div class="d-flex justify-content-between align-items-end">
                            <label class="form-label" for="news-body">Body <small class="text-muted">(Markdown)</small></label>
                            <button type="button" class="btn btn-sm btn-outline-secondary mb-2" @click="togglePreview">
                                {{ showPreview ? 'Edit' : 'Preview' }}
                            </button>
                        </div>
                        <textarea v-if="!showPreview" id="news-body" v-model="form.bodyMarkdown" class="form-control font-monospace" rows="14"></textarea>
                        <div v-else class="preview border rounded p-3" v-html="previewHtml"></div>
                    </div>
                    <div class="d-flex flex-wrap gap-2">
                        <button class="btn btn-primary" :disabled="saving || !form.title" @click="save(true)">Save &amp; publish</button>
                        <button class="btn btn-outline-primary" :disabled="saving || !form.title" @click="save(false)">Save</button>
                        <button class="btn btn-link" :disabled="saving" @click="cancel">Cancel</button>
                    </div>
                </div>
            </div>

            <!-- List -->
            <template v-else>
                <button class="btn btn-primary mb-3" @click="newPost"><i class="bi bi-plus-lg me-1"></i>New post</button>
                <div v-if="loading" class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading</span></div>
                <p v-else-if="posts.length === 0" class="text-muted">No posts yet.</p>
                <div v-else class="list-group mb-4">
                    <div v-for="post in posts" :key="post.slug" class="list-group-item d-flex flex-wrap justify-content-between align-items-center gap-2">
                        <div>
                            <span class="badge me-2" :class="post.publishedAt ? 'bg-success' : 'bg-secondary'">
                                {{ post.publishedAt ? 'Published' : 'Draft' }}
                            </span>
                            <strong>{{ post.title }}</strong>
                            <small class="text-muted ms-2">{{ post.publishedAt ? formatDate(post.publishedAt) : `updated ${formatDate(post.updatedAt)}` }}</small>
                        </div>
                        <div class="btn-group btn-group-sm">
                            <router-link v-if="post.publishedAt" :to="`/news/${post.slug}`" class="btn btn-outline-secondary">View</router-link>
                            <button class="btn btn-outline-primary" @click="edit(post)">Edit</button>
                            <button v-if="post.publishedAt" class="btn btn-outline-warning" @click="setPublished(post, false)">Unpublish</button>
                            <button v-else class="btn btn-outline-success" @click="setPublished(post, true)">Publish</button>
                            <button class="btn btn-outline-danger" @click="remove(post)">Delete</button>
                        </div>
                    </div>
                </div>

                <div class="card bg-light border-0">
                    <div class="card-body">
                        <h2 class="h6"><i class="bi bi-robot me-1"></i>Post from an AI agent (MCP)</h2>
                        <p class="small mb-2">
                            Agents can manage posts through the MCP server at <code>{{ mcpUrl }}</code>, signing in with Passi.
                            Register an <em>app</em> client under <router-link to="/OAuthApps">My OAuth apps</router-link> with redirect URI
                            <code>http://localhost:8765/callback</code>, and add its client id to the server's <code>NewsMcpClientIds</code> setting. Then:
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
