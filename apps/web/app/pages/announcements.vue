<script setup lang="ts">
import { Megaphone, Pin, Trash2 } from '@lucide/vue'
import type { AnnouncementItem } from '~/types/operations'
import { getAuthErrorMessage } from '~/utils/auth'
definePageMeta({ middleware: 'auth' })
const { request, currentOrganization } = useOperations(); const items = ref<AnnouncementItem[]>([]); const loading = ref(true); const error = ref('')
const form = reactive({ title: '', body: '', pinned: false })
const canWrite = computed(() => ['owner', 'admin', 'hr'].includes(currentOrganization.value?.role ?? ''))
const load = async () => { loading.value = true; try { items.value = await request<AnnouncementItem[]>('announcements') } catch (cause) { error.value = getAuthErrorMessage(cause) } finally { loading.value = false } }
onMounted(load)
const create = async () => { try { await request<AnnouncementItem>('announcements', { method: 'POST', body: form }); Object.assign(form, { title: '', body: '', pinned: false }); await load() } catch (cause) { error.value = getAuthErrorMessage(cause) } }
const togglePin = async (item: AnnouncementItem) => { try { await request<AnnouncementItem>(`announcements/${item.id}`, { method: 'PATCH', body: { title: item.title, body: item.body, pinned: !item.pinned } }); await load() } catch (cause) { error.value = getAuthErrorMessage(cause) } }
const remove = async (item: AnnouncementItem) => { if (!confirm(`Delete ${item.title}?`)) return; try { await request<unknown>(`announcements/${item.id}`, { method: 'DELETE' }); await load() } catch (cause) { error.value = getAuthErrorMessage(cause) } }
</script>
<template><div class="resource-page"><header class="page-header"><div><p class="eyebrow">Communication</p><h1>Announcements</h1><p>Share important updates with everyone in the workspace.</p></div></header><p v-if="error" class="form-alert">{{ error }}</p>
  <form v-if="canWrite" class="card resource-form" @submit.prevent="create"><h2>Post announcement</h2><label>Title<input v-model="form.title" required minlength="2"></label><label>Message<textarea v-model="form.body" rows="4" required minlength="2" /></label><label class="checkbox-field"><input v-model="form.pinned" type="checkbox"> Pin to the top</label><button class="button button--primary"><Megaphone :size="17" /> Publish</button></form>
  <div class="announcement-list"><p v-if="loading" class="card resource-state">Loading announcements…</p><p v-else-if="items.length === 0" class="card resource-state">No announcements yet.</p><article v-for="item in items" v-else :key="item.id" class="card announcement-card"><div class="announcement-card__header"><div><span v-if="item.pinned" class="status-pill status-pill--pinned">Pinned</span><h2>{{ item.title }}</h2><small>{{ item.authorName }} · {{ new Date(item.createdAt).toLocaleString() }}</small></div><div v-if="canWrite" class="resource-actions"><button class="icon-button" :aria-label="item.pinned ? 'Unpin announcement' : 'Pin announcement'" @click="togglePin(item)"><Pin :size="16" /></button><button class="icon-button danger-text" aria-label="Delete announcement" @click="remove(item)"><Trash2 :size="16" /></button></div></div><p>{{ item.body }}</p></article></div>
</div></template>
