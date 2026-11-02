<script>
    export default {
        data() {
            return {
                version: 1.1
            }
        },
    }
</script>
<template>

    <main>
        <div class="container">
            <div class="text-center">
                <div class="welcome-page">
                    <h1>
                        <!--<img src="~/icon.jpg">-->
                        {{ $t('home.title') }}
                        <small class="text-muted">({{ $t('home.version', { version }) }})</small>
                    </h1>

                    <ul>
                        <i18n-t keypath="home.discoveryDescription" tag="li">
                            <template #discoveryLink>
                                <a href="~/.well-known/openid-configuration">{{ $t('home.discoveryLink') }}</a>
                            </template>
                        </i18n-t>
                        <i18n-t keypath="home.claimsDescription" tag="li">
                            <template #claimsLink>
                                <a href="~/diagnostics">{{ $t('home.claimsLink') }}</a>
                            </template>
                        </i18n-t>
                    </ul>
                </div>
            </div>
        </div>
    </main>
</template>
