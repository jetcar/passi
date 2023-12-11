<script>
    import { RouterLink, RouterView } from 'vue-router'

    const API_URL = `/api/UserLoggedIn`
    export default {
        data()  {
            return {
                IsLoggedIn: false,
            }
        },
        created() {
            // fetch on init
            this.fetchData()
          },
          methods: {
              async fetchData() {
                const url = `${API_URL}`
                this.IsLoggedIn = await (await fetch(url)).json()
              }
            }
        }
</script>

<template>

    <header>
        <nav class="navbar navbar-expand-sm navbar-toggleable-sm navbar-light bg-white border-bottom box-shadow mb-3">
            <div class="container">
                <a class="navbar-brand" href="/">Passi</a>
                <button class="navbar-toggler" type="button" data-toggle="collapse" data-target=".navbar-collapse" aria-controls="navbarSupportedContent"
                        aria-expanded="false" aria-label="Toggle navigation">
                    <span class="navbar-toggler-icon" @click="ismenuvisible=!ismenuvisible"></span>
                </button>
                <div class="navbar-collapse collapse d-sm-inline-flex flex-sm-row-reverse">
                    <ul class="navbar-nav flex-grow-1">
                        <li class="nav-item">
                            <RouterLink class="nav-link text-dark" to="/">Home</RouterLink>
                        </li>
                        <li class="nav-item" >
                            <a class="nav-link text-dark" v-if="!IsLoggedIn" href="/Auth/Login">Login</a>
                        </li>
                        <li class="nav-item" >
                            <RouterLink class="nav-link text-dark" v-if="IsLoggedIn" to="/UserInfo">UserInfo</RouterLink>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link text-dark" href="/Home/DevTools">Register your website</a>
                        </li>
                        <li class="nav-item">
                            <RouterLink class="nav-link text-dark" to="/Contacts">Contacts</RouterLink>
                        </li>
                        <li class="nav-item">
                            <RouterLink class="nav-link text-dark" to="/PrivacyPolicy">Privacy Policy</RouterLink>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link text-dark" v-if="IsLoggedIn" href="/Auth/Logout">Logout</a>
                        </li>

                    </ul>
                </div>
            </div>
        </nav>
    </header>

    <RouterView />
</template>

