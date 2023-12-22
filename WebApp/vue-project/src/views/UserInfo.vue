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
                <h1 class="display-4">User Info</h1>
                <div class="form-group row">
                    <template v-for="claim in Claims">
                        <label for="email" class="col-sm-2 col-form-label">{{claim.type}}</label>
                        <div class="col-sm-10">
                            <input type="text" class="form-control" id="email" v-model="claim.value" disabled="" />
                        </div>
                    </template>
                </div>
            </div>
        </div>
    </main>


</template>

