import { useRef, useState, type ChangeEvent, type DragEvent, type KeyboardEvent } from 'react'
import { Button } from './Button'
import { Text } from './Text'
import styles from './FileUpload.module.css'

interface FileUploadProps {
  id: string
  accept?: string
  label: string
  description?: string
  selectedFile: File | null
  onFileSelect: (file: File | null) => void
  error?: string
  disabled?: boolean
}

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return `${parseFloat((bytes / k ** i).toFixed(1))} ${sizes[i]}`
}

export function FileUpload({
  id,
  accept,
  label,
  description,
  selectedFile,
  onFileSelect,
  error,
  disabled,
}: FileUploadProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [isDragging, setIsDragging] = useState(false)

  function handleClick() {
    inputRef.current?.click()
  }

  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault()
      handleClick()
    }
  }

  function handleChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0] ?? null
    onFileSelect(file)
  }

  function handleDragOver(event: DragEvent<HTMLDivElement>) {
    event.preventDefault()
    setIsDragging(true)
  }

  function handleDragLeave(event: DragEvent<HTMLDivElement>) {
    event.preventDefault()
    setIsDragging(false)
  }

  function handleDrop(event: DragEvent<HTMLDivElement>) {
    event.preventDefault()
    setIsDragging(false)
    const file = event.dataTransfer.files?.[0] ?? null
    if (file && (!accept || accept.split(',').some((type) => file.type.match(type.trim())))) {
      onFileSelect(file)
    }
  }

  function handleRemove() {
    onFileSelect(null)
    if (inputRef.current) {
      inputRef.current.value = ''
    }
  }

  return (
    <div className={styles.wrapper}>
      <Text variant="label" as="label" id={`${id}-label`}>
        {label}
      </Text>
      {description && (
        <Text variant="caption" className={styles.description}>
          {description}
        </Text>
      )}

      {!selectedFile ? (
        <div
          className={[styles.dropzone, isDragging ? styles.dragging : '', error ? styles.dropzoneError : ''].join(' ')}
          onClick={disabled ? undefined : handleClick}
          onKeyDown={disabled ? undefined : handleKeyDown}
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          onDrop={handleDrop}
          role="button"
          tabIndex={disabled ? -1 : 0}
          aria-disabled={disabled}
          aria-labelledby={`${id}-label`}
        >
          <input
            ref={inputRef}
            id={id}
            type="file"
            accept={accept}
            onChange={handleChange}
            disabled={disabled}
            className={styles.input}
          />
          <span className={styles.icon} aria-hidden="true">
            📎
          </span>
          <Text variant="body">Clique para selecionar ou arraste o arquivo aqui</Text>
          <Text variant="caption" className={styles.hint}>
            {accept ? `Formatos aceitos: ${accept}` : 'Qualquer arquivo'}
          </Text>
        </div>
      ) : (
        <div className={styles.fileCard}>
          <div className={styles.fileInfo}>
            <Text variant="body" as="span" className={styles.fileName}>
              {selectedFile.name}
            </Text>
            <Text variant="caption">{formatBytes(selectedFile.size)}</Text>
          </div>
          <Button variant="ghost" size="sm" onClick={handleRemove} disabled={disabled}>
            Remover
          </Button>
        </div>
      )}

      {error && (
        <Text variant="caption" as="span" className={styles.error} aria-live="polite">
          {error}
        </Text>
      )}
    </div>
  )
}
