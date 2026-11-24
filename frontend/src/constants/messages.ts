// M2.4: every user-facing error message lives here.
// Each one is in Spanish, has no technical codes and suggests an action.
// The backend text (in English) is never shown to the user.
// Placeholders use the form {name} and are filled with formatMessage().

export const MESSAGES = {
  // Connection
  networkError:
    'No se pudo conectar con el servidor. Verifique que la API esté en ejecución e intente de nuevo.',
  serverError: 'El servidor no pudo completar la operación. Espere un momento e intente de nuevo.',
  unexpectedError:
    'Ocurrió un problema inesperado. Intente de nuevo y, si continúa, avise al encargado.',

  // Cash session (HU-09, HU-10)
  cashRequiredToSell:
    'Debe abrir la caja antes de vender. Ingrese el monto inicial y presione Abrir caja.',
  cashAlreadyOpen: 'Ya hay una caja abierta. Ciérrela antes de abrir otra.',
  invalidOpeningAmount:
    'El monto inicial no es válido. Ingrese un monto mayor a cero con máximo dos decimales.',
  invalidCountedAmount:
    'El efectivo contado no es válido. Ingrese un monto de cero o más con máximo dos decimales.',
  cashSessionNotFound: 'No se encontró la caja. Recargue la página e intente de nuevo.',
  cashAlreadyClosed: 'La caja ya fue cerrada. Abra una caja nueva para seguir vendiendo.',
  noOpenCashToClose: 'No hay una caja abierta para cerrar. Abra una caja para empezar a vender.',

  // Products and cart (HU-01, HU-02)
  productNotFound: 'No se encontró el producto. Revise el código o busque por nombre.',
  searchTooLong: 'La búsqueda es demasiado larga. Escriba un texto más corto e intente de nuevo.',
  insufficientStock: 'No hay suficientes unidades. Disponibles: {n}. Ajuste la cantidad.',
  insufficientStockGeneric:
    'No hay suficientes unidades de un producto. Revise las cantidades e intente de nuevo.',
  outOfStock: 'Este producto no tiene unidades disponibles. Elija otra talla o color.',
  invalidQuantity: 'La cantidad no es válida. Use los botones + y − para ajustarla.',
  productUnavailable:
    'Uno de los productos ya no está disponible. Quítelo de la venta y búsquelo de nuevo.',
  saleTooLarge: 'La venta tiene demasiados productos. Quite algunas líneas e intente de nuevo.',

  // Payment (HU-05)
  paymentInsufficient: 'El pago no cubre el total. Agregue otro pago o ajuste el monto.',
  paymentNotAllowed:
    'Con tarjeta o SINPE Móvil el monto no puede superar el pendiente. Use Monto exacto o ajuste el monto.',
  invalidPaymentAmount:
    'El monto del pago no es válido. Ingrese un monto mayor a cero con máximo dos decimales.',

  // Sale registration
  saleConflict:
    'No se pudo registrar la venta porque cambió el inventario, un precio o el estado de la caja. Revise la venta e intente de nuevo.',
  saleRejected:
    'La venta no se pudo registrar. Revise los productos y los pagos e intente de nuevo.',
} as const

export type MessageKey = keyof typeof MESSAGES

export function formatMessage(
  template: string,
  values: Record<string, string | number> = {},
): string {
  return template.replace(/\{(\w+)\}/g, (placeholder, name: string) =>
    name in values ? String(values[name]) : placeholder,
  )
}
