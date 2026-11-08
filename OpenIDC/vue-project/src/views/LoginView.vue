<script>

    const API_URL = `/openidc/api/login`
    const API_CHECK_URL = `/openidc/api/check`
    const API_AUTHORIZE_URL = `/openidc/api/authorize`

    export default {
        data() {
            const loginHint = typeof this.$route.query.login_hint === "string"
                ? this.$route.query.login_hint.trim()
                : ""
            return {
                LoginInput: {
                    Username: loginHint,
                    ReturnUrl: this.$route.query.redirect_uri,
                    ClientId: this.$route.query.client_id,
                    Nonce: this.$route.query.nonce,
                },
                LoginResponse: {
                    checkColor: "red",
                    checkNumber: null,
                    sessionId:"",
                    registeredDevices: [],
                },
                errorMessage: false,
                loginForm: true,
                loading: false,
                timer: null,
                activeRequest: null,
                authorizationReady: false,
                authorizationChecking: true,
                autoLoginPending: Boolean(loginHint),
                loginToken: null,
                consent: null,
                consentAccepted: false,
            }
        },

        mounted() {
            this.validateAuthorizationRequest()
        },

        beforeUnmount() {
            this.cancelActiveRequest()
        },

        methods: {
            authorizationQuery() {
                return new URLSearchParams(window.location.search)
            },
            cancelActiveRequest() {
                clearTimeout(this.timer)
                this.timer = null
                this.activeRequest?.abort()
                this.activeRequest = null
            },
            resetLogin() {
                this.cancelActiveRequest()
                this.loginForm = true;
                this.loading = false;
                this.errorMessage = false;
                this.authorizationReady = false
                this.loginToken = null
                this.validateAuthorizationRequest()
            },
            handleCancelClick() {
                this.resetLogin()
            },
            handleCheckCancelClick() {
                this.resetLogin()
            },
            async getResponseError(response) {
                try {
                    const payload = await response.json()
                    const redirectUrl = typeof payload?.redirect_url === "string"
                        ? payload.redirect_url
                        : null
                    const errors = payload?.errors
                    if (Array.isArray(errors?.Username) && errors.Username.length) {
                        return { message: errors.Username[0], redirectUrl }
                    }
                    if (typeof errors === "string" && errors.trim()) {
                        return { message: errors, redirectUrl }
                    }
                    if (typeof payload?.error_description === "string" && payload.error_description.trim()) {
                        return { message: payload.error_description, redirectUrl }
                    }
                    if (typeof payload === "string" && payload.trim()) {
                        return { message: payload, redirectUrl }
                    }
                    return { message: this.$t('login.errors.startFailed'), redirectUrl }
                }
                catch {
                    // Fall through to a safe message when the response is not JSON.
                }
                return { message: this.$t('login.errors.startFailed'), redirectUrl: null }
            },
            async handleErrorResponse(response) {
                const error = await this.getResponseError(response)
                if (error.redirectUrl) {
                    window.location.replace(error.redirectUrl)
                    return true
                }
                this.errorMessage = error.message
                return false
            },
            async validateAuthorizationRequest(preserveError = false) {
                const request = new AbortController()
                this.activeRequest = request
                this.authorizationChecking = true
                this.authorizationReady = false
                if (!preserveError) this.errorMessage = false

                try {
                    const query = this.authorizationQuery()
                    const response = await fetch(API_AUTHORIZE_URL + "?" + query.toString(), {
                        method: "GET",
                        credentials: "include",
                        cache: "no-store",
                        signal: request.signal
                    })
                    if (this.activeRequest !== request) return

                    if (response.ok) {
                        const result = await response.json()
                        if (typeof result?.login_token !== "string" || !result.login_token) {
                            this.errorMessage = this.$t('login.errors.invalidToken')
                            return
                        }
                        this.consent = result.consent_required ? result : null
                        this.consentAccepted = false
                        if (this.consent) this.autoLoginPending = false
                        this.loginToken = result.login_token
                        this.authorizationReady = true
                        if (this.autoLoginPending) {
                            this.autoLoginPending = false
                            await this.handleLoginClick()
                        }
                    }
                    else {
                        await this.handleErrorResponse(response)
                    }
                }
                catch (error) {
                    if (error.name !== "AbortError") {
                        this.errorMessage = this.$t('login.errors.validationFailed')
                    }
                }
                finally {
                    if (this.activeRequest === request) {
                        this.activeRequest = null
                    }
                    this.authorizationChecking = false
                }
            },
            async fetchCheck() {
                const request = new AbortController()
                this.activeRequest = request
                const query = this.authorizationQuery()
                query.set("sessionId", this.LoginResponse.sessionId)

                try {
                    const response = await fetch(API_CHECK_URL + "?" + query.toString(), {
                        method: "POST",
                        credentials: "include",
                        redirect: "manual",
                        signal: request.signal
                    })
                    if (this.activeRequest !== request) return

                    if (response.ok) {
                        const result = await response.json()
                        if (this.activeRequest !== request) return

                        if (result.redirect_url) {
                            window.location.href = result.redirect_url
                        }
                        else if (result.continue) {
                            this.timer = setTimeout(() => this.fetchCheck(), 1000)
                        }
                        else {
                            this.errorMessage = this.$t('login.errors.invalidResponse')
                            this.loginForm = true
                            this.loginToken = null
                            this.authorizationReady = false
                            await this.validateAuthorizationRequest(true)
                        }
                    }
                    else if (response.type === "opaqueredirect") {
                        window.location.replace(response.url)
                    }
                    else {
                        const redirected = await this.handleErrorResponse(response)
                        this.loginForm = true
                        if (!redirected) {
                            this.loginToken = null
                            this.authorizationReady = false
                            await this.validateAuthorizationRequest(true)
                        }
                    }
                }
                catch (error) {
                    if (error.name !== "AbortError") {
                        this.errorMessage = this.$t('login.errors.checkFailed')
                        this.loginForm = true
                        this.loginToken = null
                        this.authorizationReady = false
                        await this.validateAuthorizationRequest(true)
                    }
                }
                finally {
                    if (this.activeRequest === request) {
                        this.activeRequest = null
                    }
                }
            },
            async handleLoginClick() {
                this.cancelActiveRequest()
                this.errorMessage = false
                if (!this.authorizationReady) {
                    this.errorMessage = this.authorizationChecking
                        ? this.$t('login.errors.validating')
                        : this.$t('login.errors.invalidRequest')
                    return
                }
                if (!this.loginToken) {
                    this.authorizationReady = false
                    this.errorMessage = this.$t('login.errors.revalidate')
                    await this.validateAuthorizationRequest(true)
                    return
                }
                if (this.consent && !this.consentAccepted) return
                this.LoginInput.Username = this.LoginInput.Username.trim()
                if (!this.LoginInput.Username) {
                    this.errorMessage = this.$t('login.errors.usernameRequired')
                    return
                }
                this.loading = true
                this.LoginInput.ReturnUrl = this.$route.query.redirect_uri
                this.LoginInput.ClientId = this.$route.query.client_id
                this.LoginInput.Nonce = this.$route.query.nonce
                const query = this.authorizationQuery()
                query.set("username", this.LoginInput.Username)
                const request = new AbortController()
                this.activeRequest = request

                try {
                    const response = await fetch(API_URL + "?" + query.toString(), {
                        method: "POST",
                        credentials: "include",
                        headers: {
                            "X-Passi-Login-Token": this.loginToken
                        },
                        signal: request.signal
                    })
                    if (this.activeRequest !== request) return

                    if (response.ok) {
                        this.loginToken = null
                        this.authorizationReady = false
                        this.LoginResponse = await response.json()
                        if (this.activeRequest !== request) return
                        this.loginForm = false
                        this.loading = false
                        this.fetchCheck()
                    }
                    else {
                        const redirected = await this.handleErrorResponse(response)
                        this.loginForm = true
                        if (!redirected) {
                            this.loginToken = null
                            this.authorizationReady = false
                            await this.validateAuthorizationRequest(true)
                        }
                    }
                }
                catch (error) {
                    if (error.name !== "AbortError") {
                        this.errorMessage = this.$t('login.errors.connectionFailed')
                        this.loginToken = null
                        this.authorizationReady = false
                        await this.validateAuthorizationRequest(true)
                    }
                }
                finally {
                    if (this.activeRequest === request) {
                        this.activeRequest = null
                    }
                    this.loading = false
                }
            },
        }
    }
</script>
<template>
    <main class="auth-page">
        <div class="auth-shell">
            <template v-if="loginForm">
                <section class="auth-panel auth-login-panel">
                    <a class="auth-brand" href="/" :aria-label="$t('login.homeAria')">
                        <span class="auth-brand-mark" aria-hidden="true">P</span>
                        <span>Passi</span>
                    </a>

                    <div class="auth-copy">
                        <h1>{{ $t('login.welcomeTitle') }}</h1>
                        <p>{{ $t('login.welcomeSubtitle') }}</p>
                    </div>

                    <div class="auth-alert" role="alert" v-if="errorMessage">
                        <span aria-hidden="true">!</span>
                        <div>{{errorMessage}}</div>
                    </div>

                    <form class="auth-form" @submit.prevent="handleLoginClick">
                        <section v-if="consent" class="auth-consent" :aria-label="$t('login.consent.title')">
                            <p>{{ $t('login.consent.description') }}</p>
                            <p>{{ $t('login.consent.name') }}: <bdi>{{ consent.client_name }}</bdi></p>
                            <p>{{ $t('login.consent.redirect') }}: <bdi>{{ consent.redirect_uri }}</bdi></p>
                            <p v-if="consent.resource">{{ $t('login.consent.resource') }}: <bdi>{{ consent.resource }}</bdi></p>
                            <p>{{ $t('login.consent.scopes') }}: {{ consent.scopes.join(', ') }}</p>
                            <label><input type="checkbox" v-model="consentAccepted"> {{ $t('login.consent.approve') }}</label>
                        </section>
                        <label for="username">{{ $t('login.username') }}</label>
                        <input
                            id="username"
                            name="username"
                            type="text"
                            inputmode="email"
                            autocomplete="username"
                            :placeholder="$t('login.usernamePlaceholder')"
                            v-model="LoginInput.Username"
                            :disabled="loading || authorizationChecking || !authorizationReady"
                            autofocus
                        >
                        <button class="auth-primary-button" type="submit" :disabled="loading || authorizationChecking || !authorizationReady || (consent && !consentAccepted)">
                            <span class="auth-spinner" aria-hidden="true" v-if="loading || authorizationChecking"></span>
                            {{ authorizationChecking ? $t('login.validating') : loading ? $t('login.starting') : $t('login.continue') }}
                        </button>
                    </form>

                    <p class="auth-privacy-note">
                        {{ $t('login.privacy') }}
                    </p>
                </section>

                <aside class="auth-visual" :aria-label="$t('login.passwordlessAria')">
                    <div class="auth-visual-circle" aria-hidden="true">
                        <svg class="auth-lock" viewBox="0 0 180 180" role="img" :aria-label="$t('login.secureLockAria')">
                            <defs>
                                <linearGradient id="lockBody" x1="0" y1="0" x2="1" y2="1">
                                    <stop offset="0" stop-color="#ffd05b" />
                                    <stop offset="1" stop-color="#ff855c" />
                                </linearGradient>
                            </defs>
                            <path d="M55 78V58c0-24 14-39 35-39s35 15 35 39v20" fill="none" stroke="#7c79a7" stroke-width="15" stroke-linecap="round" />
                            <rect x="38" y="70" width="104" height="80" rx="18" fill="url(#lockBody)" />
                            <circle cx="90" cy="105" r="13" fill="#353064" />
                            <rect x="84" y="105" width="12" height="28" rx="6" fill="#353064" />
                            <circle cx="139" cy="78" r="22" fill="#d1c2e8" />
                            <circle cx="145" cy="72" r="7" fill="#e9f6ff" />
                            <rect x="132" y="92" width="14" height="38" rx="7" fill="#d1c2e8" />
                        </svg>
                    </div>
                    <p>{{ $t('login.passwordStays') }}</p>
                </aside>
            </template>

            <template v-else>
                <div class="auth-panel auth-challenge-panel">
                    <section class="auth-challenge-intro">
                        <a class="auth-brand" href="/" :aria-label="$t('login.homeAria')">
                            <span class="auth-brand-mark" aria-hidden="true">P</span>
                            <span>Passi</span>
                        </a>

                        <div class="auth-copy auth-challenge-copy">
                            <h1>{{ $t('login.challenge.title') }}</h1>
                            <p>{{ $t('login.challenge.subtitle') }}</p>
                        </div>

                        <div class="auth-alert" role="alert" v-if="errorMessage">
                            <span aria-hidden="true">!</span>
                            <div>{{errorMessage}}</div>
                        </div>
                    </section>

                    <div class="auth-challenge-details">
                        <div class="device-list" :aria-label="$t('login.challenge.registeredDevicesAria')">
                            <div class="device-list-label">
                                {{ $t('login.challenge.availableDevices', LoginResponse.registeredDevices?.length || 0) }}
                            </div>
                            <div
                                class="device-card"
                                v-for="device in LoginResponse.registeredDevices"
                                :key="device"
                            >
                                <span class="device-icon" aria-hidden="true">
                                    <svg viewBox="0 0 24 24">
                                        <rect x="6" y="2" width="12" height="20" rx="2.5" fill="none" stroke="currentColor" stroke-width="2" />
                                        <path d="M10 5h4M11 19h2" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
                                    </svg>
                                </span>
                                <span class="device-name">{{ device }}</span>
                                <span class="device-status">{{ $t('login.challenge.registered') }}</span>
                            </div>
                            <div class="device-card" v-if="!LoginResponse.registeredDevices?.length">
                                <span class="device-icon" aria-hidden="true">
                                    <svg viewBox="0 0 24 24">
                                        <rect x="6" y="2" width="12" height="20" rx="2.5" fill="none" stroke="currentColor" stroke-width="2" />
                                        <path d="M10 5h4M11 19h2" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
                                    </svg>
                                </span>
                                <span class="device-name">{{ $t('login.challenge.fallbackDevice') }}</span>
                                <span class="device-status">{{ $t('login.challenge.waiting') }}</span>
                            </div>
                        </div>

                        <div class="auth-waiting-note">
                            <span class="auth-waiting-dot" aria-hidden="true"></span>
                            {{ $t('login.challenge.browserContinues') }}
                        </div>

                        <button class="auth-cancel-button" type="button" @click="handleCheckCancelClick">
                            {{ $t('login.challenge.useAnother') }}
                        </button>
                    </div>
                </div>

                <aside class="auth-challenge-number" aria-live="polite">
                    <p class="challenge-label">{{ $t('login.challenge.tapNumber') }}</p>
                    <div class="challenge-number" v-if="LoginResponse.checkNumber">
                        {{ LoginResponse.checkNumber }}
                    </div>
                    <div class="challenge-number challenge-number-loading" v-else>вЂўвЂў</div>
                    <p class="challenge-status">{{ $t('login.challenge.waitingApproval') }}</p>
                    <div class="challenge-fallback">
                        <span>{{ $t('login.challenge.olderApp', { color: LoginResponse.checkColor }) }}</span>
                        <span
                            class="color-dot"
                            :class="LoginResponse.checkColor"
                            :title="$t('login.challenge.colorTitle', { color: LoginResponse.checkColor })"
                        ></span>
                    </div>
                </aside>
            </template>
        </div>
    </main>
</template>

<style scoped>
.auth-consent { overflow-wrap: anywhere; padding: 1rem; border: 1px solid #8d87a6; border-radius: .5rem; }
.auth-consent p { margin: 0 0 .6rem; }
.auth-consent label { display: flex; align-items: flex-start; gap: .5rem; }
.auth-consent input[type="checkbox"] { width: 1.1rem; height: 1.1rem; min-height: 0; padding: 0; flex-shrink: 0; }
</style>
