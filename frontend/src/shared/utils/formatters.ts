export function unmaskDigits(value: string): string {
  return value.replace(/\D/g, '')
}

export function maskCpf(value: string): string {
  const digits = unmaskDigits(value)
  if (digits.length <= 3) return digits
  if (digits.length <= 6) return `${digits.slice(0, 3)}.${digits.slice(3)}`
  if (digits.length <= 9) return `${digits.slice(0, 3)}.${digits.slice(3, 6)}.${digits.slice(6)}`
  return `${digits.slice(0, 3)}.${digits.slice(3, 6)}.${digits.slice(6, 9)}-${digits.slice(9, 11)}`
}

export function maskPhone(value: string): string {
  const digits = unmaskDigits(value)
  if (digits.length === 0) return ''
  if (digits.length <= 2) return `(${digits}`
  if (digits.length <= 6) return `(${digits.slice(0, 2)}) ${digits.slice(2)}`
  if (digits.length <= 10) return `(${digits.slice(0, 2)}) ${digits.slice(2, 6)}-${digits.slice(6)}`
  return `(${digits.slice(0, 2)}) ${digits.slice(2, 7)}-${digits.slice(7, 11)}`
}

export function maskDate(value: string): string {
  const digits = unmaskDigits(value)
  if (digits.length <= 2) return digits
  if (digits.length <= 4) return `${digits.slice(0, 2)}/${digits.slice(2)}`
  return `${digits.slice(0, 2)}/${digits.slice(2, 4)}/${digits.slice(4, 8)}`
}

/** Converts raw BR date digits (DDMMAAAA) to ISO (YYYY-MM-DD) for the API. Passes through if malformed. */
export function brDateDigitsToIso(value: string): string {
  const digits = unmaskDigits(value)
  if (digits.length !== 8) return value
  return `${digits.slice(4, 8)}-${digits.slice(2, 4)}-${digits.slice(0, 2)}`
}

/** True when the BR date digits form a real calendar date (DDMMAAAA). Replaces the validation the
 *  native `type="date"` picker used to provide before the inputs switched to a masked text field. */
export function isValidBrDateDigits(value: string): boolean {
  const digits = unmaskDigits(value)
  if (digits.length !== 8) return false
  const day = Number(digits.slice(0, 2))
  const month = Number(digits.slice(2, 4))
  const year = Number(digits.slice(4, 8))
  if (month < 1 || month > 12 || day < 1 || day > 31) return false
  const parsed = new Date(Date.UTC(year, month - 1, day))
  return (
    parsed.getUTCFullYear() === year &&
    parsed.getUTCMonth() === month - 1 &&
    parsed.getUTCDate() === day
  )
}

/** Converts an ISO date (YYYY-MM-DD) coming from the API into raw BR digits (DDMMAAAA) for masked inputs. */
export function isoToBrDateDigits(iso: string): string {
  if (!iso) return ''
  const [year, month, day] = iso.split('-')
  if (!year || !month || !day) return ''
  return `${day}${month}${year}`
}

export function formatDateToBrazilian(dateString: string): string {
  if (!dateString) return ''
  const [year, month, day] = dateString.split('-')
  if (!year || !month || !day) return dateString
  return `${day}/${month}/${year}`
}

export function formatDateTimeToBrazilian(dateString: string): string {
  if (!dateString) return ''
  const date = new Date(dateString)
  if (Number.isNaN(date.getTime())) return dateString

  return date.toLocaleString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}
