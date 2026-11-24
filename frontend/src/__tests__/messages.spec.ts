import { describe, expect, it } from 'vitest'
import { formatMessage, MESSAGES } from '@/constants/messages'

// M2.4: every user-facing error message must
//   1. be in Spanish,
//   2. contain no technical codes (HTTP status, exception names, stack traces),
//   3. suggest an action to the cashier.
const inventory = Object.entries(MESSAGES)

const SPANISH_WORDS = /(^|\s)(el|la|los|las|de|del|un|una|que|para|no|con|y|o|en)(?=\s|[.,])/i
const ENGLISH_WORDS =
  /\b(the|error|failed|invalid|not found|please|request|server|conflict|session|sale|payment|cash|exception|undefined|null)\b/i
const TECHNICAL =
  /(\b[1-5]\d{2}\b|HTTP|stack|trace|exception|[A-Za-z]+Error\b|[a-z]+_[a-z_]+|[A-Za-z]+\(\)|::|=>|\bat\s+\w+\.)/
const ACTION_VERBS = [
  'Revise',
  'Ingrese',
  'Intente',
  'intente',
  'Ciérrela',
  'Abra',
  'Ajuste',
  'Agregue',
  'Use',
  'Quítelo',
  'Quite',
  'Busque',
  'busque',
  'Elija',
  'Recargue',
  'Verifique',
  'Presione',
  'Escriba',
  'Espere',
  'avise',
]

function hasAction(message: string): boolean {
  return ACTION_VERBS.some((verb) => new RegExp(`(^|[\\s,])${verb}(?=[\\s,.])`).test(message))
}

describe('messages inventory (M2.4)', () => {
  it('has messages to check', () => {
    expect(inventory.length).toBeGreaterThanOrEqual(20)
  })

  it.each(inventory)('%s is written in Spanish', (_key, message) => {
    expect(message).toMatch(SPANISH_WORDS)
    expect(message).not.toMatch(ENGLISH_WORDS)
  })

  it.each(inventory)('%s has no technical codes', (_key, message) => {
    expect(message).not.toMatch(TECHNICAL)
  })

  it.each(inventory)('%s suggests an action', (_key, message) => {
    expect(hasAction(message)).toBe(true)
  })

  it.each(inventory)('%s is a complete sentence', (_key, message) => {
    expect(message.trim()).toBe(message)
    expect(message.endsWith('.')).toBe(true)
    expect(message[0]).toBe(message[0]?.toUpperCase())
  })

  it('messages are unique', () => {
    const values = inventory.map(([, message]) => message)
    expect(new Set(values).size).toBe(values.length)
  })

  it('only uses the documented placeholders', () => {
    for (const [key, message] of inventory) {
      const placeholders = message.match(/\{\w+\}/g) ?? []
      const expected = key === 'insufficientStock' ? ['{n}'] : []
      expect({ key, placeholders }).toEqual({ key, placeholders: expected })
    }
  })
})

describe('formatMessage', () => {
  it('fills the available stock', () => {
    expect(formatMessage(MESSAGES.insufficientStock, { n: 3 })).toBe(
      'No hay suficientes unidades. Disponibles: 3. Ajuste la cantidad.',
    )
  })

  it('keeps unknown placeholders untouched', () => {
    expect(formatMessage('Disponibles: {n}.', {})).toBe('Disponibles: {n}.')
  })
})
