<script>
    import { RouterLink, RouterView } from 'vue-router'

    const API_URL = `/api/userloggedin`
    export default {
        data() {
            return {
                User: {
                    IsLoggedIn:false,
                    Name:""
                }
            }
        },
        created() {
            // fetch on init
            this.fetchData()
        },
        methods: {
            async fetchData() {
                const url = `${API_URL}`
                this.User = await (await fetch(url)).json()
            }
        }
    }
</script>

<template>

    <header>
        <div class="nav-page">
            <nav class="navbar navbar-expand-lg navbar-dark bg-dark">

                <a href="~/" class="navbar-brand">
                    Passi IdentityServer
                </a>
                <ul class="navbar-nav mr-auto">
                    <li class="nav-item dropdown" v-if="User.IsLoggedIn">
                        <a href="#" class="nav-link dropdown-toggle" data-toggle="dropdown">{{User.Name}} <b class="caret"></b></a>

                        <div class="dropdown-menu">
                            <a class="dropdown-item" href="Logout">Logout</a>
                        </div>
                    </li>
                </ul>

            </nav>
        </div>
    </header>

    <RouterView />
</template>

