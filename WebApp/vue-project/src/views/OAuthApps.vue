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
            if (await this.loadContext()) await this.fetchClients()
            else this.loading = false
        },

        methods: {
            async loadContext() {
                let response
                try {
                    response = await fetch(`${API_URL}/context`)
                } catch {
                    this.error = this.$t('common.networkError')
                    return false
                }
                if (response.status === 401) {
                    this.error = this.$t('oauth.loginRequired')
                    return false
                }
                let context
                try {
                    context = await response.json()
                } catch {
                    this.error = this.$t('common.invalidResponse')
                    return false
                }
                this.csrfToken = context.csrfToken
                this.csrfHeaderName = context.csrfHeaderName || this.csrfHeaderName
                this.discoveryUrl = context.discoveryUrl
                return true
            },

            async request(method, path = '', body = undefined) {
                const headers = { [this.csrfHeaderName]: this.csrfToken }
                if (body !== undefined) headers['Content-Type'] = 'application/json'
                let response
                try {
                    response = await fetch(`${API_URL}${path}`, {
                        method,
                        headers,
                        body: body === undefined ? undefined : JSON.stringify(body),
                    })
                } catch {
                    throw new Error(this.$t('common.networkError'))
                }
                if (response.status === 204) return null
                const text = await response.text()
                let data = null
                try {
                    data = text ? JSON.parse(text) : null
                } catch {
                    throw new Error(this.$t('common.invalidResponse'))
                }
                if (!response.ok) {
                    throw new Error((data && data.errors) || this.$t('common.requestFailed', { status: response.status }))
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
                if (!confirm(this.$t('oauth.confirmRotate', { name: client.displayName }))) return
                this.error = ''
                try {
                    this.issued = await this.request('POST', `/${encodeURIComponent(client.clientId)}/secret`)
                } catch (e) {
                    this.error = e.message
                }
            },

            async deleteClient(client) {
                if (!confirm(this.$t('oauth.confirmDelete', { name: client.displayName }))) return
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
                return type === 'web' ? this.$t('oauth.website') : this.$t('oauth.app')
            },
        }
    }
</script>

<template>
    <main>
        <div class="container py-4">
            <h1 class="display-6 mb-1">{{ $t('oauth.title') }}</h1>
            <p class="text-muted">
                {{ $t('oauth.intro') }}
                <span v-if="discoveryUrl">{{ $t('oauth.discovery') }} <code>{{ discoveryUrl }}</code></span>
            </p>

            <div v-if="error" class="alert alert-danger">{{ error }}</div>

            <div v-if="issued" class="alert alert-success">
                <h5 class="alert-heading">{{ $t('oauth.credentialsFor', { name: issued.displayName }) }}</h5>
                <div class="mb-1">
                    {{ $t('oauth.clientId') }}: <code>{{ issued.clientId }}</code>
                    <button class="btn btn-sm btn-link" :aria-label="$t('oauth.copyClientId')" @click="copy(issued.clientId)"><i class="bi bi-clipboard"></i></button>
                </div>
                <div v-if="issued.clientSecret">
                    {{ $t('oauth.clientSecret') }}: <code>{{ issued.clientSecret }}</code>
                    <button class="btn btn-sm btn-link" :aria-label="$t('oauth.copyClientSecret')" @click="copy(issued.clientSecret)"><i class="bi bi-clipboard"></i></button>
                    <div class="small mt-1"><strong>{{ $t('oauth.copyNow') }}</strong> — {{ $t('oauth.secretWarning') }}</div>
                </div>
                <div v-else class="small">{{ $t('oauth.appNoSecret') }}</div>
                <button class="btn btn-sm btn-outline-success mt-2" @click="issued = null">{{ $t('common.done') }}</button>
            </div>

            <div class="card mb-4">
                <div class="card-body">
                    <h5 class="card-title">{{ $t('oauth.registerNew') }}</h5>
                    <form @submit.prevent="createClient">
                        <div class="mb-3">
                            <label class="form-label" for="appName">{{ $t('oauth.name') }}</label>
                            <input id="appName" class="form-control" v-model="form.displayName" maxlength="100" required />
                        </div>
                        <div class="mb-3">
                            <label class="form-label d-block">{{ $t('oauth.type') }}</label>
                            <div class="form-check form-check-inline">
                                <input class="form-check-input" type="radio" id="typeWeb" value="web" v-model="form.clientType" />
                                <label class="form-check-label" for="typeWeb">{{ $t('oauth.webType') }}</label>
                            </div>
                            <div class="form-check form-check-inline">
                                <input class="form-check-input" type="radio" id="typeApp" value="app" v-model="form.clientType" />
                                <label class="form-check-label" for="typeApp">{{ $t('oauth.appType') }}</label>
                            </div>
                        </div>
                        <div class="mb-3">
                            <label class="form-label" for="appUris">{{ $t('oauth.redirectUris') }}</label>
                            <textarea id="appUris" class="form-control" rows="3" v-model="form.redirectUris"
                                      placeholder="https://example.com/callback" required></textarea>
                            <div class="form-text">{{ $t('oauth.redirectHint') }}</div>
                        </div>
                        <button type="submit" class="btn btn-primary" :disabled="saving">
                            <span v-if="saving" class="spinner-border spinner-border-sm me-1"></span>{{ $t('common.register') }}
                        </button>
                    </form>
                </div>
            </div>

            <h5>{{ $t('oauth.registered') }}</h5>
            <div v-if="loading" class="text-muted">{{ $t('common.loading') }}…</div>
            <div v-else-if="clients.length === 0" class="text-muted">{{ $t('oauth.noApps') }}</div>
            <div v-for="client in clients" :key="client.clientId" class="card mb-2">
                <div class="card-body">
                    <div class="d-flex justify-content-between align-items-start">
                        <div>
                            <h6 class="mb-1">{{ client.displayName }}
                                <span class="badge bg-secondary ms-1">{{ typeLabel(client.clientType) }}</span>
                            </h6>
                            <div class="small">{{ $t('oauth.clientId') }}: <code>{{ client.clientId }}</code>
                                <button class="btn btn-sm btn-link p-0 ms-1" :aria-label="$t('oauth.copyClientId')" @click="copy(client.clientId)"><i class="bi bi-clipboard"></i></button>
                            </div>
                        </div>
                        <div class="btn-group btn-group-sm">
                            <button class="btn btn-outline-secondary" @click="startEdit(client)">{{ $t('common.edit') }}</button>
                            <button v-if="client.clientType === 'web'" class="btn btn-outline-secondary" @click="rotateSecret(client)">{{ $t('oauth.newSecret') }}</button>
                            <button class="btn btn-outline-danger" @click="deleteClient(client)">{{ $t('common.delete') }}</button>
                        </div>
                    </div>

                    <ul v-if="editingId !== client.clientId" class="small mb-0 mt-2">
                        <li v-for="uri in client.redirectUris" :key="uri"><code>{{ uri }}</code></li>
                    </ul>
                    <form v-else class="mt-2" @submit.prevent="saveEdit(client)">
                        <input class="form-control form-control-sm mb-2" v-model="editForm.displayName" maxlength="100" required />
                        <textarea class="form-control form-control-sm mb-2" rows="3" v-model="editForm.redirectUris" required></textarea>
                        <button type="submit" class="btn btn-sm btn-primary me-1">{{ $t('common.save') }}</button>
                        <button type="button" class="btn btn-sm btn-outline-secondary" @click="editingId = null">{{ $t('common.cancel') }}</button>
                    </form>
                </div>
            </div>
        </div>
    </main>
</template>
