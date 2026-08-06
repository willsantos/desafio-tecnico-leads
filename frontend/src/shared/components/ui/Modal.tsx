import { useEffect, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import { Card } from './Card'
import { Button } from './Button'
import { Text } from './Text'
import styles from './Modal.module.css'

interface ModalProps {
  isOpen: boolean
  onClose: () => void
  title: string
  children: ReactNode
  footer?: ReactNode
}

export function Modal({ isOpen, onClose, title, children, footer }: ModalProps) {
  useEffect(() => {
    if (!isOpen) return

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        onClose()
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [isOpen, onClose])

  if (!isOpen) return null

  return createPortal(
    <div className={styles.overlay} role="presentation" onClick={onClose}>
      <div
        className={styles.modal}
        role="dialog"
        aria-modal="true"
        aria-labelledby="modal-title"
        onClick={(e) => e.stopPropagation()}
      >
        <Card className={styles.card}>
          <header className={styles.header}>
            <Text variant="title" as="h2" id="modal-title">
              {title}
            </Text>
            <Button variant="ghost" size="sm" onClick={onClose} aria-label="Fechar">
              ✕
            </Button>
          </header>
          <div className={styles.body}>{children}</div>
          {footer && <footer className={styles.footer}>{footer}</footer>}
        </Card>
      </div>
    </div>,
    document.body
  )
}
