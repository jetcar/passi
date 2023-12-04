<script>

    const API_URL = `/api/UserInfo`

    export default {
        data() {
           return {
               Model: {
                Identity: {
                    Claims: []
                }}
            }
        },

        created() {
            // fetch on init
            this.fetchData()
        },

        watch: {
            // re-fetch whenever currentBranch changes
            currentBranch: 'fetchData'
        },

        methods: {
            async fetchData() {
                const url = `${API_URL}`
                this.Model = await (await fetch(url)).json()
            },
            
        }
    }

</script>

<template>
    <div class="text-center">
        <h1 class="display-4">User Info</h1>
        <div class="form-group row">
            <template v-for="claim in Model.Identity.Claims">
                <label for="email" class="col-sm-2 col-form-label">{{claim.Type}}</label>
                <div class="col-sm-10">
                    <input type="text" class="form-control" id="email" value="{{claim.Value}}" disabled="" />
                </div>
            </template>
        </div>
    </div>



</template>

