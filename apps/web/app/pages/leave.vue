<script setup lang="ts">
import { Check, Palmtree, X } from '@lucide/vue'
import type { LeaveItem } from '~/types/operations'
import { getAuthErrorMessage } from '~/utils/auth'
definePageMeta({ middleware: 'auth' })
const { request, currentOrganization } = useOperations(); const { user } = useAuth(); const items = ref<LeaveItem[]>([]); const loading = ref(true); const error = ref('')
const form = reactive({ type: 'annual', startDate: '', endDate: '', reason: '' })
const canDecide = computed(() => ['owner', 'admin', 'hr', 'manager'].includes(currentOrganization.value?.role ?? ''))
const load = async () => { loading.value = true; try { items.value = await request<LeaveItem[]>('leave') } catch (cause) { error.value = getAuthErrorMessage(cause) } finally { loading.value = false } }
onMounted(load)
const create = async () => { try { await request<LeaveItem>('leave', { method: 'POST', body: form }); Object.assign(form, { type: 'annual', startDate: '', endDate: '', reason: '' }); await load() } catch (cause) { error.value = getAuthErrorMessage(cause) } }
const decide = async (item: LeaveItem, decision: 'approved' | 'rejected') => { try { await request<LeaveItem>(`leave/${item.id}/decision`, { method: 'PATCH', body: { decision, note: null } }); await load() } catch (cause) { error.value = getAuthErrorMessage(cause) } }
const cancel = async (item: LeaveItem) => { try { await request<unknown>(`leave/${item.id}/cancel`, { method: 'PATCH' }); await load() } catch (cause) { error.value = getAuthErrorMessage(cause) } }
</script>
<template><div class="resource-page"><header class="page-header"><div><p class="eyebrow">People operations</p><h1>Leave</h1><p>Request time away and manage approvals.</p></div></header><p v-if="error" class="form-alert">{{ error }}</p>
  <form class="card resource-form" @submit.prevent="create"><h2>Request leave</h2><div class="field-grid field-grid--3"><label>Type<select v-model="form.type"><option>annual</option><option>sick</option><option>personal</option><option>other</option></select></label><label>Start<input v-model="form.startDate" type="date" required></label><label>End<input v-model="form.endDate" type="date" required></label></div><label>Reason<textarea v-model="form.reason" rows="2" required minlength="2" /></label><button class="button button--primary"><Palmtree :size="17" /> Submit request</button></form>
  <div class="card table-card"><p v-if="loading" class="resource-state">Loading leave requests…</p><p v-else-if="items.length === 0" class="resource-state">No leave requests yet.</p><div v-else class="table-scroll"><table class="data-table"><thead><tr><th>Person</th><th>Dates</th><th>Type</th><th>Reason</th><th>Status</th><th /></tr></thead><tbody><tr v-for="item in items" :key="item.id"><td>{{ item.memberName }}</td><td>{{ item.startDate }} → {{ item.endDate }}</td><td>{{ item.type }}</td><td>{{ item.reason }}</td><td><span class="status-pill" :class="`status-pill--${item.status}`">{{ item.status }}</span></td><td><div v-if="item.status === 'pending'" class="resource-actions"><template v-if="canDecide && item.memberName !== user?.displayName"><button class="icon-button success-text" aria-label="Approve leave" @click="decide(item, 'approved')"><Check :size="16" /></button><button class="icon-button danger-text" aria-label="Reject leave" @click="decide(item, 'rejected')"><X :size="16" /></button></template><button v-if="item.memberName === user?.displayName" class="button button--secondary button--compact" @click="cancel(item)">Cancel</button></div></td></tr></tbody></table></div></div>
</div></template>
