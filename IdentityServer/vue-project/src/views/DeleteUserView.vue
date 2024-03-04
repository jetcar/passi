<script>

    const API_URL = `/identity/api/DeleteUser`
    const API_CONFIRM_URL = `/identity/api/DeleteConfirm`

    export default {
        data() {
            return {
                DeleteInput: {
                    Username: "",
                    Code: "",
                },
                errorMessage: false,
                loginForm: true,
                confirmForm: false,
                loading: false,
            }
        },

        methods: {
            async handleCancelClick() {
                this.errorMessage = false
                this.loginForm = true
                this.confirmForm = false
                this.loading = true;
            },
            async handleCancelConfirmClick() {
                this.errorMessage = false
                this.loginForm = false
                this.confirmForm = true
                this.loading = true;
            },
            async handleDeleteClick() {
                this.errorMessage = false
                this.loading = true
                const requestOptions = {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(this.DeleteInput)
                };

                var resp = await fetch(API_URL, requestOptions)
                if (resp.ok) {
                    resp.json().then((data) => {
                        this.LoginResponse = data
                        this.loginForm = false
                        this.loading = false
                        this.confirmForm = true
                    })

                }
                else {
                    this.errorMessage = (await resp.json()).errors.Username[0]
                    this.loginForm = true
                    this.loading = false
                }
            },
            async handleDeleteConfirmClick() {
                this.errorMessage = false
                this.loading = true
                const requestOptions = {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(this.DeleteInput)
                };

                var resp = await fetch(API_CONFIRM_URL, requestOptions)
                if (resp.ok) {
                    resp.json().then((data) => {
                        this.LoginResponse = data
                        this.loginForm = false
                        this.confirmForm = false
                        this.loading = false
                    })

                }
                else {
                    this.errorMessage = (await resp.json()).errors.Username[0]
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
                        <h1>Delete my account</h1>
                    </div>
                    <div class="error-page" v-if="errorMessage">
                        <div class="lead">
                            <h1>Error</h1>
                        </div>
                        <div class="row">
                            <div class="col-sm-6">
                                <div class="alert alert-danger">
                                    Sorry, there was an error
                                    <div>{{errorMessage}}</div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="row" v-if="loginForm">
                        <div class="col-md-6">
                            <div class="card">

                                <div class="card-body">
                                    <div v-if="loading">
                                        <div class="spinner-border" role="status">
                                            <span class="sr-only"></span>
                                        </div>
                                        <br />
                                        <button class="btn btn-secondary" name="button" @click="handleCancelClick" value="cancel">Cancel</button>
                                    </div>
                                    <br />
                                    <div class="form-group" v-if="!loading">
                                        <label for="username">Username</label>
                                        <input class="form-control" placeholder="Username" v-model="DeleteInput.username" name="username" id="username" autofocus>
                                    </div>
                                    <button class="btn btn-primary" name="button" v-if="!loading" @click="handleDeleteClick" value="delete">Delete</button>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="row" v-if="confirmForm">
                        <div class="col-md-6">
                            <div class="card">
                                <div class="card-body">
                                    <div v-if="loading">
                                        <div class="spinner-border" role="status">
                                            <span class="sr-only"></span>
                                        </div>
                                        <br />
                                        <button class="btn btn-secondary" name="button" @click="handleCancelConfirmClick" value="cancel">Cancel</button>
                                    </div>
                                    <br />
                                    <div class="form-group" v-if="!loading">
                                        <p>Check emailbox for confirmation code</p>
                                        <label for="username">Confirmation Code</label>
                                        <input class="form-control" placeholder="Code" v-model="DeleteInput.code" name="code" id="code" autofocus>
                                    </div>
                                    <button class="btn btn-primary" name="button" v-if="!loading" @click="handleDeleteConfirmClick" value="delete">Confirm Delete</button>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="row" v-if="!confirmForm && !loginForm">
                        <div class="col-md-6">
                            <div class="card">
                                <p>Account is deleted</p>
                            </div>
                        </div>
                    </div>
                </div>
                <a href='https://play.google.com/store/apps/details?id=com.passi.cloud.passi_android&pcampaignid=pcampaignidMKT-Other-global-all-co-prtnr-py-PartBadge-Mar2515-1'><img style="max-width: 200px" alt='Get it on Google Play' src='/img/en_badge_web_generic.png' /></a>
                <br />
            </div>
        </div>
    </main>
</template>