import React, { useState } from 'react';
import api from '../services/api';
import Breadcrumb from '../components/Breadcrumb';
import StatusBadge from '../components/StatusBadge';

const EXAMPLES = [
  'Quel est mon bureau actuel ?',
  "Quel matériel m'est confié ?",
  'Combien ai-je eu d’affectations au total ?',
];

const AssistantIA = () => {
  const [messages, setMessages] = useState([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const sendMessage = async (text) => {
    const message = text.trim();
    if (!message || loading) return;

    setMessages((prev) => [...prev, { role: 'user', text: message }]);
    setInput('');
    setLoading(true);
    setError('');

    try {
      const response = await api.post('/agents/me/chat', { message });
      setMessages((prev) => [...prev, { role: 'assistant', text: response.data.answer, usedAi: !!response.data.usedAi }]);
    } catch (err) {
      console.error('Agent chat error:', err);
      setError(err.response?.data?.detail || "L'assistant n'a pas pu répondre.");
    } finally { setLoading(false); }
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    sendMessage(input);
  };

  return (
    <div className="max-w-2xl">
      <Breadcrumb items={[{ label: 'Mon espace' }, { label: 'Assistant IA' }]} />

      <div className="border-b-2 border-border-subtle pb-4 mb-6">
        <h2 className="text-xl font-bold text-text-primary" style={{ fontFamily: 'var(--font-display)', fontWeight: 700 }}>
          Assistant IA
        </h2>
        <p className="mt-1 text-[12.5px] text-text-secondary">
          Posez une question sur votre bureau, votre matériel ou votre historique — l'assistant ne répond qu'à partir de vos propres données.
        </p>
      </div>

      <div className="struct-card p-5 mb-4 min-h-[220px] flex flex-col gap-3">
        {messages.length === 0 && (
          <div className="text-[12.5px] text-text-secondary italic">Aucun message pour l'instant. Essayez une des questions ci-dessous.</div>
        )}
        {messages.map((m, i) => (
          <div key={i} className={`flex ${m.role === 'user' ? 'justify-end' : 'justify-start'}`}>
            <div
              className={`max-w-[85%] px-3.5 py-2.5 text-[13px] ${
                m.role === 'user'
                  ? 'bg-primary text-white'
                  : 'bg-neutral-bg text-text-primary border-l-[3px] border-accent'
              }`}
            >
              <div>{m.text}</div>
              {m.role === 'assistant' && (
                <div className="mt-1.5">
                  <StatusBadge tone={m.usedAi ? 'success' : 'warning'}>{m.usedAi ? 'IA activée' : 'Repli mot-clé'}</StatusBadge>
                </div>
              )}
            </div>
          </div>
        ))}
        {loading && <div className="text-[12px] text-text-secondary italic">L'assistant réfléchit...</div>}
      </div>

      {error && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{error}</div>}

      <form onSubmit={handleSubmit} className="flex gap-3 items-start">
        <textarea
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={(e) => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); sendMessage(input); } }}
          placeholder="Posez votre question..."
          className="form-field flex-1 min-h-[52px] resize-none"
          rows={2}
        />
        <button
          type="submit"
          disabled={loading || !input.trim()}
          className="bg-primary px-5 py-2.5 text-[11.5px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors disabled:opacity-50 whitespace-nowrap"
          style={{ fontFamily: 'var(--font-mono)' }}
        >
          Envoyer
        </button>
      </form>
      <div className="mt-3 flex flex-wrap gap-2">
        {EXAMPLES.map((ex) => (
          <button
            key={ex}
            type="button"
            onClick={() => sendMessage(ex)}
            disabled={loading}
            className="text-[11px] text-text-secondary hover:text-primary border border-border-subtle px-2.5 py-1 transition-colors disabled:opacity-50"
          >
            {ex}
          </button>
        ))}
      </div>
    </div>
  );
};

export default AssistantIA;
