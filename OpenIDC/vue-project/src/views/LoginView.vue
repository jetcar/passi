<script>

    const API_URL = `/openidc/api/login`
    const API_CHECK_URL = `/openidc/api/check`

    export default {
        data() {
            return {
                LoginInput: {
                    Username: "",
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
            }
        },

        methods: {
            async handleCancelClick() {
                this.loginForm = true;
                this.loading = false;
                this.errorMessage = false;
                clearTimeout(this.timer)
            },
            async handleCheckCancelClick() {
                this.loginForm = true;
                this.loading = false;
                clearTimeout(this.timer)

            },
            async fetchCheck() {
                const checkRequestOptions = {
                    method: "GET",
                    credentials: "include",
                    redirect: "manual"
                };
                var queiry = new URLSearchParams(this.$route.query).toString()
                fetch(API_CHECK_URL + "?" + queiry + "&sessionId=" + this.LoginResponse.sessionId, checkRequestOptions).then(
                    (data) => {
                        if (data.ok) {
                            data.json().then((json) => {
                                if (json.redirect_url) {
                                    // Redirect to the OAuth callback URL with authorization code
                                    window.location.href = json.redirect_url;
                                }
                                else if (json.continue) {
                                    this.timer = setTimeout(() => this.fetchCheck(), 1000)
                                }


                            })
                        }
                        else if (data.type == "opaqueredirect")
                        {
                            window.location.replace(data.url)
                        }
                        else {
                            data.json().then((error) => {
                                this.errorMessage = error.errors
                            }
                            )
                        }
                    }
                ).catch((error) =>
                    this.errorMessage = error

                );

            },
            async handleLoginClick() {
                this.errorMessage = false
                this.LoginInput.Username = this.LoginInput.Username.trim()
                if (!this.LoginInput.Username) {
                    this.errorMessage = "Enter your Passi username to continue."
                    return
                }
                this.loading = true
                this.LoginInput.ReturnUrl = this.$route.query.redirect_uri
                this.LoginInput.ClientId = this.$route.query.client_id
                this.LoginInput.Nonce = this.$route.query.nonce
                const requestOptions = {
                    method: "GET",
                    credentials: "include"
                };
                var query = new URLSearchParams(this.$route.query)
                query.set("username", this.LoginInput.Username)

                var resp = await fetch(API_URL + "?" + query.toString(), requestOptions)
                if (resp.ok) {
                    resp.json().then((data) => {
                        this.LoginResponse = data
                        this.loginForm = false
                        this.loading = false
                        this.fetchCheck()
                    })

                }
                else {
                    var json = await resp.json()
                    if (json.errors.Username)
                        this.errorMessage = json.errors.Username[0]
                    else
                        this.errorMessage = json.errors
                    this.loginForm = true
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
                            :disabled="loading"
                            autofocus
                        >
                        <button class="auth-primary-button" type="submit" :disabled="loading">
                            <span class="auth-spinner" aria-hidden="true" v-if="loading"></span>
                            {{ loading ? 'Starting secure login…' : 'Continue with Passi' }}
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
                    <div class="row" v-if="!loginForm">
                        <div class="col-md-6">
                            <p>Open Passi on one of these devices to approve the login request:</p>
                            <ul v-if="LoginResponse.registeredDevices?.length">
                                <li v-for="device in LoginResponse.registeredDevices" :key="device">{{ device }}</li>
                            </ul>
                            <p class="mb-1">Tap this number in the Passi app:</p>
                            <div class="checkNumber" v-if="LoginResponse.checkNumber" aria-live="polite">{{ LoginResponse.checkNumber }}</div>
                            <div class="d-flex align-items-center small text-muted">
                                <span>Older app version? Pick this color ({{ LoginResponse.checkColor }}):</span>
                                <div class="coloredBox smallBox" :class="LoginResponse.checkColor" :title="LoginResponse.checkColor">&nbsp;</div>
                            </div>
                            <button class="btn btn-secondary" name="button" value="cancel" asp-action="CancelLogin" @click="handleCheckCancelClick">Cancel</button>
                        </div>
                    </div>
            </template>
        </div>
    </main>
</template>
