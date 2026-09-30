const API_URL = `/api/news`

export const REACTIONS = [
    { key: 'like', emoji: '👍', label: 'Like' },
    { key: 'love', emoji: '❤️', label: 'Love' },
    { key: 'celebrate', emoji: '🎉', label: 'Celebrate' },
    { key: 'rocket', emoji: '🚀', label: 'Rocket' },
    { key: 'eyes', emoji: '👀', label: 'Interesting' },
]

let contextPromise = null

/** Antiforgery token (needed for every write) and whether the signed-in user may edit news. */
export function loadContext() {
    if (!contextPromise) {
        contextPromise = fetch(`${API_URL}/context`)
            .then(r => r.json())
            .catch(() => {
                contextPromise = null
                return { csrfToken: '', csrfHeaderName: 'RequestVerificationToken', isAdmin: false }
            })
    }
    return contextPromise
}

export async function request(method, path, body = undefined) {
    const headers = {}
    if (method !== 'GET') {
        const context = await loadContext()
        headers[context.csrfHeaderName || 'RequestVerificationToken'] = context.csrfToken
    }
    if (body !== undefined) headers['Content-Type'] = 'application/json'

    const response = await fetch(`${API_URL}${path}`, {
        method,
        headers,
        body: body === undefined ? undefined : JSON.stringify(body),
    })
    if (response.status === 204) return null
    const text = await response.text()
    const data = text ? JSON.parse(text) : null
    if (!response.ok) {
        const error = new Error((data && data.errors) || `Request failed (${response.status})`)
        error.status = response.status
        throw error
    }
    return data
}

export const listPosts = (skip = 0, take = 10) => request('GET', `?skip=${skip}&take=${take}`)
export const getPost = slug => request('GET', `/${encodeURIComponent(slug)}`)
export const setReaction = (slug, reaction, on) =>
    request(on ? 'PUT' : 'DELETE', `/${encodeURIComponent(slug)}/reactions/${reaction}`)

export const admin = {
    list: () => request('GET', '/admin/posts'),
    create: post => request('POST', '/admin/posts', post),
    update: (slug, post) => request('PUT', `/admin/posts/${encodeURIComponent(slug)}`, post),
    publish: slug => request('POST', `/admin/posts/${encodeURIComponent(slug)}/publish`),
    unpublish: slug => request('POST', `/admin/posts/${encodeURIComponent(slug)}/unpublish`),
    remove: slug => request('DELETE', `/admin/posts/${encodeURIComponent(slug)}`),
    preview: bodyMarkdown => request('POST', '/admin/preview', { bodyMarkdown }),
}

export function formatDate(value) {
    if (!value) return ''
    return new Date(value).toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' })
}
