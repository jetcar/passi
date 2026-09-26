<script>
    const API_URL = `/api/oauthclients`

    const emptyForm = () => ({ displayName: '', clientType: 'web', redirectUris: '' })

    export default {
        data() {
            return {
                clients: [],
                loading: true,
                error: '',
                discoveryUrl: '',
                csrfHeaderName: 'RequestVerificationToken',
                csrfToken: '',
                form: emptyForm(),
                saving: false,
                // Credentials returned once on create / secret rotation; never retrievable again.
                issued: null,
                editingId: null,
                editForm: { displayName: '', redirectUris: '' },
            }
        },

        async created() {
            await this.loadContext()
            await this.fetchClients()
        },

        methods: {
            async loadContext() {
                const response = await fetch(`${API_URL}/context`)
                if (response.status === 401) {
                    this.error = 'Please log in to manage your applications.'
                    return
                }
                const context = await response.json()
                this.csrfToken = context.csrfToken
                this.csrfHeaderName = context.csrfHeaderName || this.csrfHeaderName
                this.discoveryUrl = context.discoveryUrl
            },

            async request(method, path = '', body = undefined) {
                const headers = { [this.csrfHeaderName]: this.csrfToken }
                if (body !== undefined) headers['Content-Type'] = 'application/json'
                const response = await fetch(`${API_URL}${path}`, {
                    method,
                    headers,
                    body: body === undefined ? undefined : JSON.stringify(body),
                })
                if (response.status === 204) return null
                const text = await response.text()
                const data = text ? JSON.parse(text) : null
                if (!response.ok) {
                    throw new Error((data && data.errors) || `Request failed (${response.status})`)
                }
                return data
            },

            async fetchClients() {
                this.loading = true
                try {
                    this.clients = (await this.request('GET')) || []
                } catch (e) {
                    this.error = this.error || e.message
                } finally {
                    this.loading = false
                }
            },

            splitUris(text) {
                return text.split(/\r?\n/).map(x => x.trim()).filter(x => x)
            },

            async createClient() {
                this.error = ''
                this.saving = true
                try {
                    const created = await this.request('POST', '', {
                        displayName: this.form.displayName,
                        clientType: this.form.clientType,
                        redirectUris: this.splitUris(this.form.redirectUris),
                    })
                    this.issued = created
                    this.form = emptyForm()
                    await this.fetchClients()
                } catch (e) {
                    this.error = e.message
                } finally {
                    this.saving = false
                }
            },

            startEdit(client) {
                this.editingId = client.clientId
                this.editForm = { displayName: client.displayName, redirectUris: client.redirectUris.join('\n') }
            },

            async saveEdit(client) {
                this.error = ''
                try {
                    await this.request('PUT', `/${encodeURIComponent(client.clientId)}`, {
                        displayName: this.editForm.displayName,
                        redirectUris: this.splitUris(this.editForm.redirectUris),
                    })
                    this.editingId = null
                    await this.fetchClients()
                } catch (e) {
                    this.error = e.message
                }
            },

            async rotateSecret(client) {
                if (!confirm(`Generate a new secret for "${client.displayName}"? The current secret stops working immediately.`)) return
                this.error = ''
                try {
                    this.issued = await this.request('POST', `/${encodeURIComponent(client.clientId)}/secret`)
                } catch (e) {
                    this.error = e.message
                }
            },

            async deleteClient(client) {
                if (!confirm(`Delete "${client.displayName}"? Users will no longer be able to sign in to it with Passi.`)) return
                this.error = ''
                try {
                    await this.request('DELETE', `/${encodeURIComponent(client.clientId)}`)
                    if (this.issued && this.issued.clientId === client.clientId) this.issued = null
                    await this.fetchClients()
                } catch (e) {
                    this.error = e.message
                }
            },

            copy(text) {
                navigator.clipboard?.writeText(text)
            },

            typeLabel(type) {
                return type === 'web' ? 'Website' : 'App'
            },
        }
    }
</script>

<template>
    <main>
        <div class="container py-4">
            <h1 class="display-6 mb-1">My OAuth apps</h1>
            <p class="text-muted">
                Register your own website or application to let users sign in with Passi.
                <span v-if="discoveryUrl">OpenID Connect discovery: <code>{{ discoveryUrl }}</code></span>
            </p>

            <div v-if="error" class="alert alert-danger">{{ error }}</div>

            <div v-if="issued" class="alert alert-success">
                <h5 class="alert-heading">Credentials for "{{ issued.displayName }}"</h5>
                <div class="mb-1">
                    Client ID: <code>{{ issued.clientId }}</code>
                    <button class="btn btn-sm btn-link" @click="copy(issued.clientId)"><i class="bi bi-clipboard"></i></button>
                </div>
                <div v-if="issued.clientSecret">
                    Client secret: <code>{{ issued.clientSecret }}</code>
                    <button class="btn btn-sm btn-link" @click="copy(issued.clientSecret)"><i class="bi bi-clipboard"></i></button>
                    <div class="small mt-1"><strong>Copy it now</strong> — it is stored hashed and will not be shown again.</div>
                </div>
                <div v-else class="small">App clients have no secret; use the authorization code flow with PKCE.</div>
                <button class="btn btn-sm btn-outline-success mt-2" @click="issued = null">Done</button>
            </div>

            <div class="card mb-4">
                <div class="card-body">
                    <h5 class="card-title">Register a new application</h5>
                    <form @submit.prevent="createClient">
                        <div class="mb-3">
                            <label class="form-label" for="appName">Name</label>
                            <input id="appName" class="form-control" v-model="form.displayName" maxlength="100" required />
                        </div>
                        <div class="mb-3">
                            <label class="form-label d-block">Type</label>
                            <div class="form-check form-check-inline">
                                <input class="form-check-input" type="radio" id="typeWeb" value="web" v-model="form.clientType" />
                                <label class="form-check-label" for="typeWeb">Website (server-side, gets a client secret)</label>
                            </div>
                            <div class="form-check form-check-inline">
                                <input class="form-check-input" type="radio" id="typeApp" value="app" v-model="form.clientType" />
                                <label class="form-check-label" for="typeApp">App / SPA (no secret, PKCE)</label>
                            </div>
                        </div>
                        <div class="mb-3">
                            <label class="form-label" for="appUris">Redirect URIs (one per line)</label>
                            <textarea id="appUris" class="form-control" rows="3" v-model="form.redirectUris"
                                      placeholder="https://example.com/callback" required></textarea>
                            <div class="form-text">Must use https (http is allowed only for localhost).</div>
                        </div>
                        <button type="submit" class="btn btn-primary" :disabled="saving">
                            <span v-if="saving" class="spinner-border spinner-border-sm me-1"></span>Register
                        </button>
                    </form>
                </div>
            </div>

            <h5>Registered applications</h5>
            <div v-if="loading" class="text-muted">Loading…</div>
            <div v-else-if="clients.length === 0" class="text-muted">You haven't registered any applications yet.</div>
            <div v-for="client in clients" :key="client.clientId" class="card mb-2">
                <div class="card-body">
                    <div class="d-flex justify-content-between align-items-start">
                        <div>
                            <h6 class="mb-1">{{ client.displayName }}
                                <span class="badge bg-secondary ms-1">{{ typeLabel(client.clientType) }}</span>
                            </h6>
                            <div class="small">Client ID: <code>{{ client.clientId }}</code>
                                <button class="btn btn-sm btn-link p-0 ms-1" @click="copy(client.clientId)"><i class="bi bi-clipboard"></i></button>
                            </div>
                        </div>
                        <div class="btn-group btn-group-sm">
                            <button class="btn btn-outline-secondary" @click="startEdit(client)">Edit</button>
                            <button v-if="client.clientType === 'web'" class="btn btn-outline-secondary" @click="rotateSecret(client)">New secret</button>
                            <button class="btn btn-outline-danger" @click="deleteClient(client)">Delete</button>
                        </div>
                    </div>

                    <ul v-if="editingId !== client.clientId" class="small mb-0 mt-2">
                        <li v-for="uri in client.redirectUris" :key="uri"><code>{{ uri }}</code></li>
                    </ul>
                    <form v-else class="mt-2" @submit.prevent="saveEdit(client)">
                        <input class="form-control form-control-sm mb-2" v-model="editForm.displayName" maxlength="100" required />
                        <textarea class="form-control form-control-sm mb-2" rows="3" v-model="editForm.redirectUris" required></textarea>
                        <button type="submit" class="btn btn-sm btn-primary me-1">Save</button>
                        <button type="button" class="btn btn-sm btn-outline-secondary" @click="editingId = null">Cancel</button>
                    </form>
                </div>
            </div>
        </div>
    </main>
</template>
