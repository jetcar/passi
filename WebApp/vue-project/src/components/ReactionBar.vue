<script>
    import { REACTIONS, setReaction } from '../news/newsApi'

    export default {
        props: {
            slug: { type: String, required: true },
            reactions: { type: Object, required: true },
            compact: { type: Boolean, default: false },
        },
        data() {
            return {
                state: this.reactions,
                busy: false,
                error: '',
                options: REACTIONS,
            }
        },
        watch: {
            reactions(value) { this.state = value },
        },
        methods: {
            isMine(key) {
                return (this.state.mine || []).includes(key)
            },
            async toggle(key) {
                if (this.busy) return
                this.busy = true
                this.error = ''
                try {
                    this.state = await setReaction(this.slug, key, !this.isMine(key))
                } catch (e) {
                    this.error = e.status === 429 ? 'Slow down a little, try again in a minute.' : 'Could not save your reaction.'
                } finally {
                    this.busy = false
                }
            },
        },
    }
</script>

<template>
    <div class="reaction-bar" :class="{ compact }">
        <button v-for="option in options" :key="option.key" type="button"
                class="reaction" :class="{ mine: isMine(option.key) }"
                :title="option.label" :aria-label="option.label" :aria-pressed="isMine(option.key)"
                :disabled="busy" @click.prevent.stop="toggle(option.key)">
            <span class="emoji">{{ option.emoji }}</span>
            <span class="count">{{ state.counts?.[option.key] || 0 }}</span>
        </button>
        <small v-if="error" class="text-danger ms-2">{{ error }}</small>
    </div>
</template>

<style scoped>
.reaction-bar {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0.5rem;
}

.reaction {
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    padding: 0.3rem 0.75rem;
    border: 1px solid #dee2e6;
    border-radius: 999px;
    background: white;
    font-size: 0.95rem;
    line-height: 1.2;
    transition: all 0.2s ease;
    cursor: pointer;
}

.reaction:hover:not(:disabled) {
    border-color: #667eea;
    transform: translateY(-1px);
}

.reaction.mine {
    background: rgba(102, 126, 234, 0.12);
    border-color: #667eea;
    color: #3f51b5;
    font-weight: 600;
}

.reaction:disabled {
    cursor: default;
    opacity: 0.8;
}

.compact .reaction {
    padding: 0.15rem 0.55rem;
    font-size: 0.85rem;
}
</style>
