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
                    query: new URLSearchParams(this.$route.query).toString()
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
            }
        },

        mounted() {
            this.validateAuthorizationRequest()
        },

        beforeUnmount() {
            this.cancelActiveRequest()
        },

        methods: {
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
                    return { message: "Passi could not start the login. Please try again.", redirectUrl }
                }
                catch {
                    // Fall through to a safe message when the response is not JSON.
                }
                return { message: "Passi could not start the login. Please try again.", redirectUrl: null }
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
            async validateAuthorizationRequest() {
                const request = new AbortController()
                this.activeRequest = request
                this.authorizationChecking = true
                this.authorizationReady = false
                this.errorMessage = false

                try {
                    const query = new URLSearchParams(this.$route.query)
                    const response = await fetch(API_AUTHORIZE_URL + "?" + query.toString(), {
                        method: "GET",
                        credentials: "include",
                        signal: request.signal
                    })
                    if (this.activeRequest !== request) return

                    if (response.ok) {
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
                        this.errorMessage = "Passi could not validate the login request. Please try again."
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
                const query = new URLSearchParams(this.$route.query)
                query.set("sessionId", this.LoginResponse.sessionId)

                try {
                    const response = await fetch(API_CHECK_URL + "?" + query.toString(), {
                        method: "GET",
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
                            this.errorMessage = "Passi returned an invalid login response. Please try again."
                            this.loginForm = true
                        }
                    }
                    else if (response.type === "opaqueredirect") {
                        window.location.replace(response.url)
                    }
                    else {
                        await this.handleErrorResponse(response)
                        this.loginForm = true
                    }
                }
                catch (error) {
                    if (error.name !== "AbortError") {
                        this.errorMessage = "Passi could not check the login request. Please try again."
                        this.loginForm = true
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
                        ? "Passi is validating the login request."
                        : "This login request is invalid."
                    return
                }
                this.LoginInput.Username = this.LoginInput.Username.trim()
                if (!this.LoginInput.Username) {
                    this.errorMessage = "Enter your Passi username to continue."
                    return
                }
                this.loading = true
                this.LoginInput.ReturnUrl = this.$route.query.redirect_uri
                this.LoginInput.ClientId = this.$route.query.client_id
                this.LoginInput.Nonce = this.$route.query.nonce
                const query = new URLSearchParams(this.$route.query)
                query.set("username", this.LoginInput.Username)
                const request = new AbortController()
                this.activeRequest = request

                try {
                    const response = await fetch(API_URL + "?" + query.toString(), {
                        method: "GET",
                        credentials: "include",
                        signal: request.signal
                    })
                    if (this.activeRequest !== request) return

                    if (response.ok) {
                        this.LoginResponse = await response.json()
                        if (this.activeRequest !== request) return
                        this.loginForm = false
                        this.loading = false
                        this.fetchCheck()
                    }
                    else {
                        await this.handleErrorResponse(response)
                        this.loginForm = true
                    }
                }
                catch (error) {
                    if (error.name !== "AbortError") {
                        this.errorMessage = "Passi could not start the login. Check your connection and try again."
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
                    <a class="auth-brand" href="/" aria-label="Passi home">
                        <span class="auth-brand-mark" aria-hidden="true">P</span>
                        <span>Passi</span>
                    </a>

                    <div class="auth-copy">
                        <h1>Welcome back</h1>
                        <p>Enter your Passi username to continue securely.</p>
                    </div>

                    <div class="auth-alert" role="alert" v-if="errorMessage">
                        <span aria-hidden="true">!</span>
                        <div>{{errorMessage}}</div>
                    </div>

                    <form class="auth-form" @submit.prevent="handleLoginClick">
                        <label for="username">Username</label>
                        <input
                            id="username"
                            name="username"
                            type="text"
                            inputmode="email"
                            autocomplete="username"
                            placeholder="alex@example.com"
                            v-model="LoginInput.Username"
                            :disabled="loading || authorizationChecking || !authorizationReady"
                            autofocus
                        >
                        <button class="auth-primary-button" type="submit" :disabled="loading || authorizationChecking || !authorizationReady">
                            <span class="auth-spinner" aria-hidden="true" v-if="loading || authorizationChecking"></span>
                            {{ authorizationChecking ? 'Validating request…' : loading ? 'Starting secure login…' : 'Continue with Passi' }}
                        </button>
                    </form>

                    <p class="auth-privacy-note">
                        No password is sent or stored in this browser.
                    </p>
                </section>

                <aside class="auth-visual" aria-label="Passwordless authentication">
                    <div class="auth-visual-circle" aria-hidden="true">
                        <svg class="auth-lock" viewBox="0 0 180 180" role="img" aria-label="Secure lock">
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
                    <p>Your password stays with you.</p>
                </aside>
            </template>

            <template v-else>
                <section class="auth-panel auth-challenge-panel">
                    <a class="auth-brand" href="/" aria-label="Passi home">
                        <span class="auth-brand-mark" aria-hidden="true">P</span>
                        <span>Passi</span>
                    </a>

                    <div class="auth-copy auth-challenge-copy">
                        <h1>Check your phone</h1>
                        <p>Open Passi on a registered device and approve this login request.</p>
                    </div>

                    <div class="auth-alert" role="alert" v-if="errorMessage">
                        <span aria-hidden="true">!</span>
                        <div>{{errorMessage}}</div>
                    </div>

                    <div class="device-list" aria-label="Registered devices">
                        <div class="device-list-label">
                            {{ LoginResponse.registeredDevices?.length === 1 ? 'Available device' : 'Available devices' }}
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
                            <span class="device-status">Registered</span>
                        </div>
                        <div class="device-card" v-if="!LoginResponse.registeredDevices?.length">
                            <span class="device-icon" aria-hidden="true">
                                <svg viewBox="0 0 24 24">
                                    <rect x="6" y="2" width="12" height="20" rx="2.5" fill="none" stroke="currentColor" stroke-width="2" />
                                    <path d="M10 5h4M11 19h2" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
                                </svg>
                            </span>
                            <span class="device-name">Your Passi app</span>
                            <span class="device-status">Waiting</span>
                        </div>
                    </div>

                    <div class="auth-waiting-note">
                        <span class="auth-waiting-dot" aria-hidden="true"></span>
                        The browser will continue automatically after approval.
                    </div>

                    <button class="auth-cancel-button" type="button" @click="handleCheckCancelClick">
                        Use another account
                    </button>
                </section>

                <aside class="auth-challenge-number" aria-live="polite">
                    <p class="challenge-label">Tap this number in Passi</p>
                    <div class="challenge-number" v-if="LoginResponse.checkNumber">
                        {{ LoginResponse.checkNumber }}
                    </div>
                    <div class="challenge-number challenge-number-loading" v-else>••</div>
                    <p class="challenge-status">Waiting for approval…</p>
                    <div class="challenge-fallback">
                        <span>Older app? Match {{ LoginResponse.checkColor }}</span>
                        <span
                            class="color-dot"
                            :class="LoginResponse.checkColor"
                            :title="LoginResponse.checkColor"
                        ></span>
                    </div>
                </aside>
            </template>
        </div>
    </main>
</template>
