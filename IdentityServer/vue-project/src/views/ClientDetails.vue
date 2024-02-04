<script>

    const API_URL = `/api/UserInfo`

    export default {
        data() {
            return {
                Claims: []
            }
        },

        created() {
            this.fetchData()
        },
        methods: {
            async fetchData() {
                const url = `${API_URL}`
                this.Claims = await (await fetch(url)).json()
            },

        }
    }

</script>

<template>
    <main>
        <div class="container">
            <div class="text-center">

            </div>
        </div>
    </main>


</template>

