<template>
    <main>
        <div class="container py-5 privacy-policy">
            <h1 class="display-4 text-center mb-4">{{ $t('privacy.title') }}</h1>
            <p>{{ $t('privacy.intro') }}</p>

            <h2 class="h4 mt-4">{{ $t('privacy.dataTitle') }}</h2>
            <p>{{ $t('privacy.dataIntro') }}</p>
            <ul>
                <li>{{ $t('privacy.ip') }}</li>
                <li>{{ $t('privacy.email') }}</li>
                <li>{{ $t('privacy.domains') }}</li>
            </ul>

            <h2 class="h4 mt-4">{{ $t('privacy.reasonsTitle') }}</h2>
            <p>{{ $t('privacy.reasonsIntro') }}</p>
            <ul><li>{{ $t('privacy.reason') }}</li></ul>

            <h2 class="h4 mt-4">{{ $t('privacy.securityTitle') }}</h2>
            <p>{{ $t('privacy.security') }}</p>

            <h2 class="h4 mt-4">{{ $t('privacy.cookiesTitle') }}</h2>
            <p>{{ $t('privacy.cookies1') }}</p>
            <p>{{ $t('privacy.cookies2') }}</p>
            <p>{{ $t('privacy.cookies3') }}</p>

            <h2 class="h4 mt-4">{{ $t('privacy.linksTitle') }}</h2>
            <p>{{ $t('privacy.links') }}</p>

            <h2 class="h4 mt-4">{{ $t('privacy.restrictTitle') }}</h2>
            <p>{{ $t('privacy.restrict1') }}</p>
            <p>{{ $t('privacy.restrict2') }}</p>
            <p>{{ $t('privacy.restrict3') }}</p>
        </div>
    </main>
</template>

<style scoped>
.privacy-policy { max-width: 900px; }
.privacy-policy p, .privacy-policy li { line-height: 1.7; }
</style>
