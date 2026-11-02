<script setup>
    import { computed, reactive } from 'vue'
    import { RouterView } from 'vue-router'
    import { useI18n } from 'vue-i18n'
    import { setLocale, supportedLocales } from './i18n'

    const API_URL = `/api/userloggedin`
    const { locale, t } = useI18n()
    const User = reactive({ IsLoggedIn: false, Name: '' })
    const selectedLocale = computed({
        get: () => locale.value,
        set: (value) => setLocale(value),
    })

    async function fetchData() {
        Object.assign(User, await (await fetch(API_URL)).json())
    }

    fetchData()
</script>

<template>
    <div class="locale-switcher">
        <label for="openidc-locale">{{ t('locale.selectorLabel') }}</label>
        <select id="openidc-locale" v-model="selectedLocale">
            <option v-for="localeCode in supportedLocales" :key="localeCode" :value="localeCode">
                {{ t(`locale.languages.${localeCode}`) }}
            </option>
        </select>
    </div>

    <header v-if="!['Login', 'Login2'].includes($route.name)">
        <div class="nav-page">
            <nav class="navbar navbar-expand-lg navbar-dark bg-dark">

                <a href="~/" class="navbar-brand">
                    Passi OpenIDC
                </a>
                <ul class="navbar-nav mr-auto">
                    <li class="nav-item dropdown" v-if="User.IsLoggedIn">
                        <a href="#" class="nav-link dropdown-toggle" data-toggle="dropdown">{{User.Name}} <b class="caret"></b></a>

                        <div class="dropdown-menu">
                            <a class="dropdown-item" href="Logout">{{ t('nav.logout') }}</a>
                        </div>
                    </li>
                </ul>
            </nav>
        </div>
    </header>

    <RouterView />
</template>
