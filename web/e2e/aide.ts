import { expect, type Page } from '@playwright/test'

// Comptes seedés du dev (appsettings.Development.json) ; surchargeables pour une autre pile.
export const UTILISATEUR = process.env.E2E_UTILISATEUR ?? 'alain'
export const MOT_DE_PASSE = process.env.E2E_MOT_DE_PASSE ?? '45234523'

export function dansNJours(n: number): string {
  const date = new Date()
  date.setDate(date.getDate() + n)
  const mois = String(date.getMonth() + 1).padStart(2, '0')
  const jour = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${mois}-${jour}`
}

export async function seConnecter(page: Page) {
  await page.goto('/')
  await page.getByPlaceholder('alain ou ariane').fill(UTILISATEUR)
  await page.getByLabel('Mot de passe').fill(MOT_DE_PASSE)
  await page.getByRole('button', { name: 'Entrer' }).click()
  await expect(page.getByRole('link', { name: 'Tâches' })).toBeVisible()
}

export async function ouvrirConsoleTaches(page: Page) {
  await page.getByRole('link', { name: 'Tâches' }).click()
  await expect(page.getByRole('heading', { name: 'Toutes les tâches' })).toBeVisible()
  // La console mémorise la vue ; les parcours travaillent dans la vue Liste.
  await page.getByRole('button', { name: 'Liste' }).click()
}

/** La rangée cliquable de la vue Liste : un <button> dont le nom commence par le titre. */
export function rangeeConsole(page: Page, titre: string) {
  return page.getByRole('button', { name: new RegExp(`^${titre}`) })
}

/** La case à cocher de complétion d'une rangée de la console. */
export function caseCompleter(page: Page, titre: string) {
  return page.getByRole('button', { name: `Compléter ${titre}` })
}

/** Suppression deux temps via ConfirmerSuppression (garde anti double-clic de 300 ms). */
export async function confirmerSuppression(page: Page, ariaLabel: string) {
  await page.getByRole('button', { name: ariaLabel, exact: true }).click()
  await page.waitForTimeout(400)
  await page.getByRole('button', { name: `${ariaLabel} — confirmer` }).click()
}
