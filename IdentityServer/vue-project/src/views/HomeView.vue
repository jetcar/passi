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
                        Welcome to Passi IdentityServer
                        <small class="text-muted">(version {{version}})</small>
                    </h1>

                    <ul>
                        <li>
                            IdentityServer publishes a
                            <a href="~/.well-known/openid-configuration">discovery document</a>
                            where you can find metadata and links to all the endpoints, key material, etc.
                        </li>
                        <li>
                            Click <a href="~/diagnostics">here</a> to see the claims for your current session.
                        </li>

                    </ul>
                </div>
            </div>
        </div>
    </main>
</template>
