import { act, renderHook } from '@testing-library/react'
import { afterEach, expect, test } from 'vitest'
import { effacerErreur, signalerErreur, useErreurCourante } from '@/lib/erreurs'

afterEach(() => act(() => effacerErreur()))

test('la dernière erreur signalée est publiée aux abonnés, effacer la retire', () => {
  const { result } = renderHook(() => useErreurCourante())
  expect(result.current).toBeNull()

  act(() => signalerErreur('Le serveur a hoqueté.'))
  expect(result.current?.message).toBe('Le serveur a hoqueté.')

  act(() => signalerErreur('Deuxième problème.'))
  expect(result.current?.message).toBe('Deuxième problème.')

  act(() => effacerErreur())
  expect(result.current).toBeNull()
})

test('resignaler le même message donne un nouvel id — la bannière peut se réafficher', () => {
  const { result } = renderHook(() => useErreurCourante())

  act(() => signalerErreur('Pareil.'))
  const premierId = result.current?.id
  act(() => signalerErreur('Pareil.'))

  expect(result.current?.id).not.toBe(premierId)
})
