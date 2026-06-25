--
-- PostgreSQL database dump
--


-- Dumped from database version 18.4 (Debian 18.4-1.pgdg13+1)
-- Dumped by pg_dump version 18.4 (Ubuntu 18.4-1.pgdg24.04+1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: btree_gist; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS btree_gist WITH SCHEMA public;


--
-- Name: EXTENSION btree_gist; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON EXTENSION btree_gist IS 'support for indexing common datatypes in GiST';


--
-- Name: uuid-ossp; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS "uuid-ossp" WITH SCHEMA public;


--
-- Name: EXTENSION "uuid-ossp"; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON EXTENSION "uuid-ossp" IS 'generate universally unique identifiers (UUIDs)';


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: favoritos; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.favoritos (
    usuario_id uuid NOT NULL,
    inmueble_id uuid NOT NULL,
    fecha_agregado timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: inmuebles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.inmuebles (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    propietario_id uuid NOT NULL,
    titulo character varying(200) NOT NULL,
    descripcion text,
    ubicacion character varying(255) NOT NULL,
    latitud numeric(9,6),
    longitud numeric(9,6),
    tarifa_por_noche numeric(12,2) NOT NULL,
    activo boolean DEFAULT true NOT NULL,
    fecha_creacion timestamp with time zone DEFAULT now() NOT NULL,
    url_imagen character varying(500),
    CONSTRAINT inmuebles_tarifa_por_noche_check CHECK ((tarifa_por_noche > (0)::numeric))
);


--
-- Name: kyc_verifications; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.kyc_verifications (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    usuario_id uuid NOT NULL,
    estado character varying(20) DEFAULT 'Pendiente'::character varying NOT NULL,
    nombres_extraidos character varying(150),
    apellidos_extraidos character varying(150),
    numero_documento_extraido character varying(50),
    fecha_nacimiento_extraida date,
    confianza_ocr numeric(4,3) DEFAULT 0 NOT NULL,
    nombre_coincide boolean,
    documento_coincide boolean,
    fecha_nacimiento_coincide boolean,
    razones jsonb DEFAULT '[]'::jsonb NOT NULL,
    fecha_verificacion timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT chk_kyc_confianza CHECK (((confianza_ocr >= (0)::numeric) AND (confianza_ocr <= (1)::numeric))),
    CONSTRAINT chk_kyc_estado CHECK (((estado)::text = ANY ((ARRAY['Pendiente'::character varying, 'Aprobado'::character varying, 'Rechazado'::character varying, 'EnRevision'::character varying])::text[])))
);


--
-- Name: reservas; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.reservas (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    inmueble_id uuid NOT NULL,
    usuario_id uuid NOT NULL,
    fecha_inicio date NOT NULL,
    fecha_fin date NOT NULL,
    hora_checkin time without time zone DEFAULT '14:00:00'::time without time zone NOT NULL,
    hora_checkout time without time zone DEFAULT '12:00:00'::time without time zone NOT NULL,
    precio_total numeric(12,2) NOT NULL,
    estado character varying(20) DEFAULT 'Pendiente'::character varying NOT NULL,
    fecha_creacion timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT chk_reserva_estado CHECK (((estado)::text = ANY ((ARRAY['Pendiente'::character varying, 'Confirmada'::character varying, 'Cancelada'::character varying, 'Completada'::character varying])::text[]))),
    CONSTRAINT chk_reserva_fechas CHECK ((fecha_fin > fecha_inicio)),
    CONSTRAINT reservas_precio_total_check CHECK ((precio_total >= (0)::numeric))
);


--
-- Name: usuarios; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.usuarios (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    email character varying(255) NOT NULL,
    password_hash character varying(255) NOT NULL,
    nombres character varying(150) NOT NULL,
    apellidos character varying(150) NOT NULL,
    numero_documento character varying(50) NOT NULL,
    fecha_nacimiento date NOT NULL,
    rol character varying(20) DEFAULT 'Huesped'::character varying NOT NULL,
    kyc_aprobado boolean DEFAULT false NOT NULL,
    fecha_registro timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT chk_usuarios_fecha_nacimiento CHECK ((fecha_nacimiento <= CURRENT_DATE)),
    CONSTRAINT chk_usuarios_rol CHECK (((rol)::text = ANY ((ARRAY['Huesped'::character varying, 'Propietario'::character varying])::text[])))
);


--
-- Data for Name: favoritos; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.favoritos (usuario_id, inmueble_id, fecha_agregado) FROM stdin;
\.


--
-- Data for Name: inmuebles; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.inmuebles (id, propietario_id, titulo, descripcion, ubicacion, latitud, longitud, tarifa_por_noche, activo, fecha_creacion, url_imagen) FROM stdin;
313d4d0c-f6fb-4852-ad3b-223fce4d6932	7575e106-dca0-4cf9-9430-d8dd391ea716	Casa en el poblado	Una increible casa para pasar tus dias	medellin	123.000000	42.000000	12000.00	t	2026-06-23 23:56:12.582561+00	https://images.pexels.com/photos/323780/pexels-photo-323780.jpeg
d054cc72-0b50-4c6c-9dfa-f7705dc0cc78	7575e106-dca0-4cf9-9430-d8dd391ea716	casa	asdad	medellin	12.000000	120.000000	12000.00	t	2026-06-23 23:57:41.27389+00	https://images.pexels.com/photos/106399/pexels-photo-106399.jpeg
edf420c8-f4b4-4d76-9818-0709ad661555	7575e106-dca0-4cf9-9430-d8dd391ea716	Casa linda	una casa muy linda	Cali	12.000000	12.000000	12021.00	t	2026-06-24 01:47:53.655253+00	https://images.pexels.com/photos/1396122/pexels-photo-1396122.jpeg
\.


--
-- Data for Name: kyc_verifications; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.kyc_verifications (id, usuario_id, estado, nombres_extraidos, apellidos_extraidos, numero_documento_extraido, fecha_nacimiento_extraida, confianza_ocr, nombre_coincide, documento_coincide, fecha_nacimiento_coincide, razones, fecha_verificacion) FROM stdin;
c0190232-6168-48b7-b88d-cabc4e0f3749	a46f3093-c8c7-4468-8f76-7b85fdc5403d	Aprobado	SANTIAGO	BOTERO	1018237701	2007-06-04	0.950	t	t	t	[]	2026-06-24 00:28:35.725974+00
\.


--
-- Data for Name: reservas; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.reservas (id, inmueble_id, usuario_id, fecha_inicio, fecha_fin, hora_checkin, hora_checkout, precio_total, estado, fecha_creacion) FROM stdin;
0641babf-9200-4efb-bb86-252b089b87b3	d054cc72-0b50-4c6c-9dfa-f7705dc0cc78	a46f3093-c8c7-4468-8f76-7b85fdc5403d	2026-06-24	2026-06-27	14:00:00	12:00:00	366666.00	Cancelada	2026-06-24 00:35:26.342353+00
84501d75-d91d-43f2-bc99-25898bbad2ae	d054cc72-0b50-4c6c-9dfa-f7705dc0cc78	a46f3093-c8c7-4468-8f76-7b85fdc5403d	2026-06-24	2026-06-25	14:00:00	12:00:00	122222.00	Cancelada	2026-06-24 00:35:53.889124+00
057f1210-ab15-40ba-82e5-7b45fab1923a	d054cc72-0b50-4c6c-9dfa-f7705dc0cc78	a46f3093-c8c7-4468-8f76-7b85fdc5403d	2026-06-25	2026-06-30	14:00:00	12:00:00	11665.00	Cancelada	2026-06-24 01:39:11.775593+00
7c15f82b-48b3-455c-b57d-7a9e9f49fa3f	d054cc72-0b50-4c6c-9dfa-f7705dc0cc78	a46f3093-c8c7-4468-8f76-7b85fdc5403d	2026-07-01	2026-07-04	14:00:00	12:00:00	6999.00	Cancelada	2026-06-24 01:37:37.902049+00
fb241a0a-efc6-4b16-b0ee-cd1cbce2b92c	edf420c8-f4b4-4d76-9818-0709ad661555	a46f3093-c8c7-4468-8f76-7b85fdc5403d	2026-06-24	2026-06-25	14:00:00	12:00:00	12021.00	Cancelada	2026-06-24 01:48:34.98931+00
\.


--
-- Data for Name: usuarios; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.usuarios (id, email, password_hash, nombres, apellidos, numero_documento, fecha_nacimiento, rol, kyc_aprobado, fecha_registro) FROM stdin;
7575e106-dca0-4cf9-9430-d8dd391ea716	boterosantiago40@gmail.com	$2a$11$xeyuwiAOI5yVCVHjlFyMKu5Z5ZWOtEB31YXMEvcfYk.6sx/pesBXG	test	2	1018237700	2007-06-04	Propietario	f	2026-06-23 23:54:29.194457+00
a46f3093-c8c7-4468-8f76-7b85fdc5403d	santiagoboterito@gmail.com	$2a$11$uTsLtf1mKYszBqNe846UC.WcL9eeJHAO0DD8wQKAU2pjcqiKAndmC	Santiago	Botero	1018237701	2007-06-04	Huesped	t	2026-06-23 22:54:36.074564+00
1c6df047-f770-4610-b8da-1278a0588ee6	admin@test.com	$2a$11$HPEFxlpwPEd8O6CZKPkCEekLA9gxoUXlJvZVjSa1cWrBigbkgTvHO	Admin	admin	123345678	2005-02-12	Huesped	f	2026-06-24 00:57:05.616499+00
\.


--
-- Name: reservas excl_reservas_solapadas; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.reservas
    ADD CONSTRAINT excl_reservas_solapadas EXCLUDE USING gist (inmueble_id WITH =, daterange(fecha_inicio, fecha_fin, '[)'::text) WITH &&) WHERE (((estado)::text = ANY ((ARRAY['Pendiente'::character varying, 'Confirmada'::character varying])::text[])));


--
-- Name: favoritos favoritos_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.favoritos
    ADD CONSTRAINT favoritos_pkey PRIMARY KEY (usuario_id, inmueble_id);


--
-- Name: inmuebles inmuebles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inmuebles
    ADD CONSTRAINT inmuebles_pkey PRIMARY KEY (id);


--
-- Name: kyc_verifications kyc_verifications_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kyc_verifications
    ADD CONSTRAINT kyc_verifications_pkey PRIMARY KEY (id);


--
-- Name: kyc_verifications kyc_verifications_usuario_id_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kyc_verifications
    ADD CONSTRAINT kyc_verifications_usuario_id_key UNIQUE (usuario_id);


--
-- Name: reservas reservas_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.reservas
    ADD CONSTRAINT reservas_pkey PRIMARY KEY (id);


--
-- Name: usuarios uq_usuarios_numero_documento; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT uq_usuarios_numero_documento UNIQUE (numero_documento);


--
-- Name: usuarios usuarios_email_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT usuarios_email_key UNIQUE (email);


--
-- Name: usuarios usuarios_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT usuarios_pkey PRIMARY KEY (id);


--
-- Name: idx_favoritos_usuario; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_favoritos_usuario ON public.favoritos USING btree (usuario_id);


--
-- Name: idx_inmuebles_propietario; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_inmuebles_propietario ON public.inmuebles USING btree (propietario_id);


--
-- Name: idx_inmuebles_ubicacion; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_inmuebles_ubicacion ON public.inmuebles USING btree (ubicacion);


--
-- Name: idx_kyc_estado; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_kyc_estado ON public.kyc_verifications USING btree (estado);


--
-- Name: idx_kyc_usuario_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_kyc_usuario_id ON public.kyc_verifications USING btree (usuario_id);


--
-- Name: idx_reservas_inmueble; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_reservas_inmueble ON public.reservas USING btree (inmueble_id);


--
-- Name: idx_reservas_rango_fechas; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_reservas_rango_fechas ON public.reservas USING btree (inmueble_id, fecha_inicio, fecha_fin);


--
-- Name: idx_reservas_usuario; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_reservas_usuario ON public.reservas USING btree (usuario_id);


--
-- Name: idx_usuarios_email; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_usuarios_email ON public.usuarios USING btree (email);


--
-- Name: idx_usuarios_numero_documento; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_usuarios_numero_documento ON public.usuarios USING btree (numero_documento);


--
-- Name: favoritos fk_favorito_inmueble; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.favoritos
    ADD CONSTRAINT fk_favorito_inmueble FOREIGN KEY (inmueble_id) REFERENCES public.inmuebles(id) ON DELETE CASCADE;


--
-- Name: favoritos fk_favorito_usuario; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.favoritos
    ADD CONSTRAINT fk_favorito_usuario FOREIGN KEY (usuario_id) REFERENCES public.usuarios(id) ON DELETE CASCADE;


--
-- Name: inmuebles fk_inmueble_propietario; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inmuebles
    ADD CONSTRAINT fk_inmueble_propietario FOREIGN KEY (propietario_id) REFERENCES public.usuarios(id) ON DELETE RESTRICT;


--
-- Name: kyc_verifications fk_kyc_usuario; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.kyc_verifications
    ADD CONSTRAINT fk_kyc_usuario FOREIGN KEY (usuario_id) REFERENCES public.usuarios(id) ON DELETE CASCADE;


--
-- Name: reservas fk_reserva_inmueble; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.reservas
    ADD CONSTRAINT fk_reserva_inmueble FOREIGN KEY (inmueble_id) REFERENCES public.inmuebles(id) ON DELETE RESTRICT;


--
-- Name: reservas fk_reserva_usuario; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.reservas
    ADD CONSTRAINT fk_reserva_usuario FOREIGN KEY (usuario_id) REFERENCES public.usuarios(id) ON DELETE RESTRICT;


--
-- PostgreSQL database dump complete
--


