import{_ as a,c as s,o as c,a as e}from"./index-1d82e7d8.js";const n="/api/UserInfo",o={data(){return{Claims:[]}},created(){this.fetchData()},methods:{async fetchData(){const t=`${n}`;this.Claims=await(await fetch(t)).json()}}},r=e("div",{class:"container"},[e("div",{class:"text-center"})],-1),i=[r];function _(t,l,d,f,h,p){return c(),s("main",null,i)}const u=a(o,[["render",_]]);export{u as default};
