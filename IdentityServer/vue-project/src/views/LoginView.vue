<script>

    const API_URL = `/identity/api/login`
    const API_CHECK_URL = `/identity/api/check`

    export default {
        data() {
            return {
                LoginInput: {
                    Username: "",
                    ReturnUrl: this.$route.query.returnUrl,
                },
                LoginResponse: {
                    checkColor: "red"

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
                clearTimeout(this.timer)
            },
            async handleCheckCancelClick() {
                this.loginForm = true;
                this.loading = false;
            },
            async fetchCheck() {
                const checkRequestOptions = {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(this.LoginResponse)
                };
                fetch(API_CHECK_URL, checkRequestOptions).then(
                    (data) => {
                        if (data.ok) {
                            data.json().then((json) => {
                                if (json.redirect) {
                                    window.location.replace(json.redirect)
                                }
                                else {
                                    this.timer = setTimeout(() => this.fetchCheck(), 1000)
                                }

                            })
                        }
                        else {
                            resp.json().then((error) => {
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
                this.loading = true
                this.LoginInput.ReturnUrl = this.$route.query.ReturnUrl
                const requestOptions = {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(this.LoginInput)
                };

                var resp = await fetch(API_URL, requestOptions)
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
    <main>
        <div class="container">
            <div>
                <div>
                    <div class="lead">
                        <h1>Login</h1>
                    </div>
                    <div class="error-page" v-if="errorMessage">
                        <div class="lead">
                            <h1>Error</h1>
                        </div>
                        <div class="row">
                            <div class="col-sm-6">
                                <div class="alert alert-danger">
                                    Sorry, there was an error:
                                    <div>{{errorMessage}}</div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="row" v-if="loginForm">
                        <div class="col-md-6">
                            <div class="card">

                                <div class="card-body">
                                    <div class="spinner-border" role="status" v-if="loading">
                                        <span class="sr-only"></span>
                                    </div>
                                    <br />
                                    <div class="form-group" v-if="!loading">
                                        <label for="username">Username</label>
                                        <input class="form-control" placeholder="Username" v-model="LoginInput.username" name="username" id="username" autofocus>
                                    </div>
                                    <button class="btn btn-primary" name="button" v-if="!loading" @click="handleLoginClick" value="login">Login</button>
                                    <button class="btn btn-secondary" name="button" @click="handleCancelClick" value="cancel">Cancel</button>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="row" v-if="!loginForm">
                        <div class="col-md-6">
                            <label for="CheckColor"></label>
                            <div class="coloredBox" :class="LoginResponse.checkColor">&nbsp;</div>
                            <button class="btn btn-secondary" name="button" value="cancel" asp-action="CancelLogin" @click="handleCheckCancelClick">Cancel</button>
                        </div>
                    </div>
                </div>
                <a href='https://play.google.com/store/apps/details?id=com.passi.cloud.passi_android&pcampaignid=pcampaignidMKT-Other-global-all-co-prtnr-py-PartBadge-Mar2515-1'><img style="max-width: 200px" alt='Get it on Google Play' src='/img/en_badge_web_generic.png' /></a>
                <br />
            </div>
        </div>
    </main>
</template>