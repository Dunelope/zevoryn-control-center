import { FormEvent, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../lib/api';
import type { Product } from '../lib/types';

export function Products() {
  const [products, setProducts] = useState<Product[]>([]); const [counts, setCounts] = useState<Record<string, number>>({});
  const [name, setName] = useState(''); const [slug, setSlug] = useState(''); const [description, setDescription] = useState(''); const [error, setError] = useState('');
  const load = async () => { try { const items = await api.products(); setProducts(items); const pairs = await Promise.all(items.map(async p => [p.id, (await api.environments(p.id)).length] as const)); setCounts(Object.fromEntries(pairs)); } catch (e) { setError((e as Error).message); } };
  useEffect(() => { void load(); }, []);
  async function submit(e: FormEvent) { e.preventDefault(); try { await api.createProduct({ name, slug, description: description || null }); setName(''); setSlug(''); setDescription(''); await load(); } catch (e) { setError((e as Error).message); } }
  return <section><div className="page-heading"><div><p className="eyebrow">Catalog</p><h2>Products</h2></div></div>{error && <p className="error">{error}</p>}<div className="grid"><div className="card"><h3>Registered products</h3>{products.length === 0 ? <p className="muted">No products registered yet. Add the first SaaS product to begin.</p> : <ul className="rows">{products.map(p => <li key={p.id}><div><Link to={`/products/${p.id}`}>{p.name}</Link><span>{p.slug} · {counts[p.id] ?? 0} environment{counts[p.id] === 1 ? '' : 's'}</span></div><span className={`status ${p.status.toLowerCase()}`}>{p.status}</span></li>)}</ul>}</div><form className="card form" onSubmit={submit}><h3>Add product</h3><input required placeholder="Name" value={name} onChange={e => setName(e.target.value)}/><input required placeholder="slug" value={slug} onChange={e => setSlug(e.target.value)}/><textarea placeholder="Description (optional)" value={description} onChange={e => setDescription(e.target.value)}/><button type="submit">Create product</button></form></div></section>;
}
