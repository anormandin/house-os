/**
 * État d'erreur d'une requête de page : distinct d'un état vide (« La maison
 * respire » sur un backend en panne serait un mensonge), avec bouton de réessai.
 */
export default function ErreurChargement({
  quoi,
  onReessayer,
}: {
  quoi: string
  onReessayer: () => void
}) {
  return (
    <div className="flex flex-col items-center gap-3 rounded-[20px] border-2 border-dashed border-rouge/50 px-5 py-8 text-center">
      <p className="text-sm font-bold text-rouge">
        Impossible de charger {quoi} — le serveur n’a pas répondu.
      </p>
      <button
        type="button"
        onClick={onReessayer}
        className="rounded-full bg-orange px-4 py-2 text-sm font-bold text-carte"
      >
        Réessayer
      </button>
    </div>
  )
}
