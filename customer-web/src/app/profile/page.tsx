'use client';
import { useEffect, useMemo, useState } from 'react';
import { AuthEmpty, useSessionUser } from '@/components/account/RequireAuth';
import { AccountShell } from '@/components/account/AccountShell';
import { marketplaceApi } from '@/lib/api/apiClient';
import { Icon } from '@/components/ui/Icon';
import { useI18n } from '@/lib/i18n/client';
import { saveSession } from '@/lib/api/session';
import { mediaUrl } from '@/lib/media/url';

const DEFAULT_AVATARS = Array.from({ length: 12 }, (_, i) => `/avatars/avatar-${i + 1}.svg`);

export default function ProfilePage(){
  const { t } = useI18n();
  const { user, ready } = useSessionUser();
  const [profile,setProfile]=useState<any>({});
  const [msg,setMsg]=useState('');
  const [error,setError]=useState('');
  const [loading,setLoading]=useState(true);
  const [uploading,setUploading]=useState(false);
  const [avatarUrl,setAvatarUrl]=useState('');
  const [verificationCode,setVerificationCode]=useState('');
  const [codeSent,setCodeSent]=useState(false);
  const [sendingCode,setSendingCode]=useState(false);
  const [verifyingCode,setVerifyingCode]=useState(false);

  useEffect(()=>{
    if(!user?.id){setLoading(false);return;}
    let off=false;
    marketplaceApi.me(user.id)
      .then(p=>{ if(!off){ setProfile(p); setAvatarUrl(p?.avatarUrl || user.avatarUrl || DEFAULT_AVATARS[0]); } })
      .catch(()=>{ if(!off){ setProfile(user); setAvatarUrl(user.avatarUrl || DEFAULT_AVATARS[0]); } })
      .finally(()=>!off&&setLoading(false));
    return()=>{off=true};
  },[user?.id]);

  const p = useMemo(() => ({...user,...profile, avatarUrl: avatarUrl || profile?.avatarUrl || user?.avatarUrl}), [user, profile, avatarUrl]);
  const avatarSrc = mediaUrl(p.avatarUrl) || '';
  const initials = String(p.name??'U').split(' ').map((x:string)=>x[0]).join('').slice(0,2).toUpperCase();

  async function uploadAvatar(file?: File | null){
    if(!file || !user?.id) return;
    setUploading(true); setError(''); setMsg('');
    try{
      const fd = new FormData();
      fd.append('File', file);
      fd.append('OwnerId', user.id);
      const media:any = await marketplaceApi.uploadMedia(fd);
      setAvatarUrl(media.url);
      setMsg(t('avatarUploaded'));
    } catch(err:any){ setError(err.message ?? t('uploadFailed')); }
    finally{ setUploading(false); }
  }

  async function sendPhoneCode(){
    if(!user?.id) return;
    const phone = String(profile?.phone ?? (user as any).phone ?? '').trim();
    if(!phone){ setError(t('phoneRequiredBeforeVerify')); return; }
    setSendingCode(true); setError(''); setMsg('');
    try{
      await marketplaceApi.sendPhoneVerification(user.id);
      setCodeSent(true); setMsg(t('codeSent'));
    }catch(err:any){ setError(err.message ?? t('saveFailed')); }
    finally{ setSendingCode(false); }
  }

  async function verifyPhoneCode(){
    if(!user?.id || !verificationCode.trim()) return;
    setVerifyingCode(true); setError(''); setMsg('');
    try{
      const result:any = await marketplaceApi.verifyPhone(user.id, verificationCode.trim());
      const updated = result?.user || {...p, phoneVerified:true};
      setProfile((current:any)=>({...current,...updated,phoneVerified:true}));
      saveSession({user:{...user,...updated,phoneVerified:true}});
      setVerificationCode(''); setCodeSent(false); setMsg(t('phoneVerifiedSuccess'));
    }catch(err:any){ setError(err.message ?? t('saveFailed')); }
    finally{ setVerifyingCode(false); }
  }

  async function submit(e:React.FormEvent<HTMLFormElement>){
    e.preventDefault(); if(!user?.id)return;
    setError(''); setMsg('');
    const f=new FormData(e.currentTarget);
    const name = String(f.get('name') ?? '').trim();
    if(!name){ setError(t('fullNameRequired')); return; }
    try{
      const data=await marketplaceApi.saveMe(user.id,{name,phone:f.get('phone'),location:f.get('location'),avatarUrl});
      setProfile(data); setAvatarUrl(data?.avatarUrl || avatarUrl); saveSession({user:data}); setMsg(t('profileSaved'));
    }catch(err:any){ setError(err.message ?? t('saveFailed')); }
  }

  if(!ready) return null;
  if(!user) return <AuthEmpty title={t('viewProfileTitle')} text={t('profileAuthText')} />;
  return <AccountShell user={user} title={t('viewProfileTitle')} subtitle={t('profileSubtitle')}>
    <div className="profile-grid-v3">
      <section className="public-profile-card-v3">
        <div className="profile-cover-v3"></div>
        <div className="profile-avatar-large-v3 has-image">{avatarSrc ? <img src={avatarSrc} alt={p.name || t('avatar')} /> : initials}</div>
        <h2>{p.name??t('customer')}</h2><p>{p.location??'San Jose, CA'} · {t('memberProfile')}</p>
        <div className="profile-score-v3"><span><b>{p.rating??0}</b><small>{t('rating')}</small></span><span><b>{p.reviewCount??0}</b><small>{t('reviews')}</small></span><span><b>{p.trustScore??0}</b><small>{t('trust')}</small></span></div>
        <div className="trust-list-modern"><span className={p.emailVerified?'ok':''}><Icon name="mail" size={15}/> {t('email')} {p.emailVerified?t('verified'):t('pending')}</span><span className={p.phoneVerified?'ok':''}><Icon name="phone" size={15}/> {t('phone')} {p.phoneVerified?t('verified'):t('pending')}</span><span className={p.idVerified?'ok':''}><Icon name="shield" size={15}/> {t('identity')} {p.idVerified?t('verified'):t('pending')}</span></div>
      </section>
      <form className="profile-form-v3" onSubmit={submit}>
        <h2>{t('personalInformation')}</h2><p>{t('profileInfoText')}</p>
        {loading?<div className="empty-state-v3">{t('loadingProfile')}</div>:null}
        <div className="avatar-picker-v3">
          <div className="avatar-preview-v3">{avatarSrc ? <img src={avatarSrc} alt={t('avatarPreview')}/> : initials}</div>
          <div>
            <strong>{t('profileAvatar')}</strong>
            <p>{t('profileAvatarText')}</p>
            <label className="avatar-upload-btn">{uploading ? t('uploading') : t('uploadAvatar')}<input type="file" accept="image/*" onChange={e=>uploadAvatar(e.target.files?.[0])}/></label>
          </div>
        </div>
        <div className="default-avatar-list-v3">
          {DEFAULT_AVATARS.map(a => <button type="button" key={a} className={avatarUrl===a?'active':''} onClick={()=>setAvatarUrl(a)}><img src={mediaUrl(a) || a} alt={t('defaultAvatar')} /></button>)}
        </div>
        <label>{t('fullName')} <b>*</b><input name="name" required defaultValue={p.name??''}/></label>
        <label>{t('email')}<input value={p.email??''} disabled /></label>
        <div className="auth-two-v2"><label>{t('phone')}<input name="phone" defaultValue={p.phone??''}/></label><label>{t('location')}<input name="location" defaultValue={p.location??''}/></label></div>
        <section className="phone-verification-card-v3">
          <div>
            <strong>{t('phoneVerification')}</strong>
            <p>{t('phoneVerificationText')}</p>
            <span className={p.phoneVerified?'verification-status verified':'verification-status'}>{p.phoneVerified?t('alreadyVerified'):t('notVerified')}</span>
          </div>
          {!p.phoneVerified && <div className="phone-verification-actions-v3">
            <button type="button" className="secondary-button" disabled={sendingCode} onClick={sendPhoneCode}>{sendingCode?t('sendingCode'):(codeSent?t('resendVerificationCode'):t('sendVerificationCode'))}</button>
            {codeSent && <div className="phone-code-line-v3"><input inputMode="numeric" autoComplete="one-time-code" maxLength={8} value={verificationCode} placeholder={t('verificationCode')} onChange={e=>setVerificationCode(e.target.value.replace(/\D/g,''))}/><button type="button" className="primary-button" disabled={verifyingCode||!verificationCode} onClick={verifyPhoneCode}>{verifyingCode?t('verifyingCode'):t('verifyPhone')}</button></div>}
          </div>}
        </section>
        <input type="hidden" name="avatarUrl" value={avatarUrl}/>
        <button className="primary-button" type="submit">{t('saveProfile')}</button>{msg&&<p className="success-message-v2">{msg}</p>}{error&&<p className="form-error">{error}</p>}
      </form>
    </div>
  </AccountShell>;
}
